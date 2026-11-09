using System.Net;
using System.Reflection;
using System.Text;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Commands.Transaction.Packing.NS.Returns;
using Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>Exercise public save paths with an in-memory HTTP boundary: no live NetSuite traffic.</summary>
public sealed class FulfillmentSaveStabilityTests : IDisposable
{
    private readonly CapturingHandler _http = new();
    private readonly NetSuiteApiClientService _service;
    private readonly Dictionary<string, string?> _flags = new();
    private readonly FieldInfo _token = typeof(NetSuiteApiClientService).GetField("_accessToken", BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly FieldInfo _expiry = typeof(NetSuiteApiClientService).GetField("_tokenExpiryTime", BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly object? _oldToken;
    private readonly object? _oldExpiry;

    public FulfillmentSaveStabilityTests()
    {
        SetFlag(NetSuiteApiClientService.UseRestletForTOIFVariable, "false");
        SetFlag(NetSuiteApiClientService.ReconciliationFlagVariable, "true");
        SetFlag(NetSuiteApiClientService.FailClosedRevalidationFlagVariable, "true");
        SetFlag(NetSuiteApiClientService.TransformVerificationFlagVariable, "false");
        _oldToken = _token.GetValue(null);
        _oldExpiry = _expiry.GetValue(null);
        _token.SetValue(null, "offline-test-token");
        _expiry.SetValue(null, DateTime.UtcNow.AddMinutes(10));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(_http));
        _service = new NetSuiteApiClientService(new HttpContextAccessor(), null!, NullLogger<NetSuiteApiClientService>.Instance);
    }

    private void SetFlag(string name, string value)
    {
        _flags[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        NetSuiteApiClientService.ResetHttpClientForTests();
        _token.SetValue(null, _oldToken);
        _expiry.SetValue(null, _oldExpiry);
        foreach (var flag in _flags) Environment.SetEnvironmentVariable(flag.Key, flag.Value);
    }

    private static PostTransferOrderDTO Line(int lineId, bool bad, int bin = 777) => new()
    {
        NetsuiteOrderInternalId = 42, OrderNumber = "STR-42", LineSequenceNumber = lineId,
        MaterialCode = $"SKU-{lineId}", NetsuiteMaterialInternalId = lineId,
        LineQuantity = 10, UoMRate = 1, ScannedQuantity = 1,
        IsBad = bad, IsLocationUsedBin = true, NetsuiteMaterialPrefferedBinId = bin
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mixed_orders_with_colliding_line_keys_fail_before_dedupe_or_HTTP(bool restlet)
    {
        var first = Line(1, false);
        var second = Line(1, false);
        second.NetsuiteOrderInternalId = 43;
        second.ScannedQuantity = 2;
        var error = await Assert.ThrowsAsync<Exception>(() => restlet
            ? _service.SaveTOItemFulfillmentViaRestlet([first, second])
            : _service.SaveTOItemFulfillment([first, second]));
        Assert.Contains("exactly one transfer order", error.Message);
        Assert.Equal(0, _http.Reads + _http.Writes);
    }

    [Fact]
    public async Task Returns_mixed_order_input_is_rejected_before_HTTP()
    {
        var error = await Assert.ThrowsAsync<Exception>(() => _service.SaveReturnsItemFulfillment([
            new() { NetsuiteOrderInternalId = 42, LineSequenceNumber = 1, ScannedQuantity = 1 },
            new() { NetsuiteOrderInternalId = 43, LineSequenceNumber = 1, ScannedQuantity = 2 }
        ]));
        Assert.Contains("exactly one return order", error.Message);
        Assert.Equal(0, _http.Reads + _http.Writes);
    }

    [Fact]
    public async Task TO_missing_GOOD_bin_prevents_the_otherwise_valid_BAD_write()
    {
        var error = await Assert.ThrowsAsync<Exception>(() => _service.SaveTOItemFulfillment([
            Line(1, bad: true), Line(2, bad: false, bin: 0)
        ]));
        Assert.Contains("no bin is resolved", error.Message);
        Assert.Contains("SKU-2", error.Message);
        Assert.Equal(1, _http.Reads);
        Assert.Equal(0, _http.Writes);
    }

    [Fact]
    public async Task RTS_missing_GOOD_bin_prevents_the_otherwise_valid_BAD_write()
    {
        var error = await Assert.ThrowsAsync<Exception>(() => _service.SaveVRAItemFulfillment([
            new() { NetsuiteOrderInternalId = 42, LineSequenceNumber = 1, ScannedQuantity = 1, IsBad = true, IsLocationUsedBin = true, NetsuiteMaterialPrefferedBinId = 777 },
            new() { NetsuiteOrderInternalId = 42, LineSequenceNumber = 2, ScannedQuantity = 1, MaterialCode = "GOOD-SKU", IsLocationUsedBin = true }
        ]));
        Assert.Contains("no bin is resolved", error.Message);
        Assert.Equal(0, _http.Writes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Valid_bin_and_non_bin_TO_saves_still_post_both_quality_buckets(bool bins)
    {
        var bad = Line(1, true, bins ? 777 : 0);
        var good = Line(2, false, bins ? 777 : 0);
        bad.IsLocationUsedBin = good.IsLocationUsedBin = bins;
        Assert.True(await _service.SaveTOItemFulfillment([bad, good]));
        Assert.Equal(1, _http.Reads);
        Assert.Equal(2, _http.Writes);
    }

    [Fact]
    public async Task Command_handlers_reject_original_mixed_orders_before_exemption_lookup()
    {
        // Null dependencies deliberately prove validation does not attempt any
        // exemption lookup or posting for invalid input.
        var to = new PostTransferOrderIFCmdHandler(null!, null!, NullLogger<PostTransferOrderIFCmdHandler>.Instance);
        var returns = new PostReturnsIFCmdHandler(null!, null!, NullLogger<PostReturnsIFCmdHandler>.Instance);
        var toResult = await to.Handle(new PostTransferOrderIFCmd([
            new() { NetsuiteOrderInternalId = 42 }, new() { NetsuiteOrderInternalId = 43 }
        ]), default);
        var returnResult = await returns.Handle(new PostReturnsIFCmd([
            new() { NetsuiteOrderInternalId = 42 }, new() { NetsuiteOrderInternalId = 43 }
        ]), default);
        Assert.False(toResult.Success);
        Assert.Contains("Exactly one transfer order", toResult.ErrorMessage);
        Assert.False(returnResult.Success);
        Assert.Contains("Exactly one return order", returnResult.ErrorMessage);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int Reads { get; private set; }
        public int Writes { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.Contains("suiteql", StringComparison.OrdinalIgnoreCase))
            {
                Reads++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"items":[
                          {"lineid":1,"item":1,"itemcode":"SKU-1","linequantity":10,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":1},
                          {"lineid":2,"item":2,"itemcode":"SKU-2","linequantity":10,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":1}
                        ],"totalResults":2,"hasMore":false}
                        """, Encoding.UTF8, "application/json")
                });
            }
            Writes++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
