using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Fully_backordered_filler_is_not_posted_and_other_filler_is_explicitly_deselected(bool reconciliation, bool bad)
    {
        SetFlag(NetSuiteApiClientService.ReconciliationFlagVariable, reconciliation.ToString());
        _http.FreshResponse = BackorderedOrder;
        var scanned = Line(5, bad);
        var backordered = Line(2, false);
        var unscanned = Line(8, false);
        backordered.ScannedQuantity = unscanned.ScannedQuantity = 0;
        // Submitted snapshot deliberately still calls line 2 packable.
        Assert.True(await _service.SaveTOItemFulfillment([scanned, backordered, unscanned]));

        using var payload = JsonDocument.Parse(Assert.Single(_http.PostedBodies));
        var items = payload.RootElement.GetProperty("item").GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal([5, 8], items.Select(x => x.GetProperty("orderLine").GetInt32()));
        Assert.True(items[0].GetProperty("itemreceive").GetBoolean());
        Assert.Equal(1m, items[0].GetProperty("quantity").GetDecimal());
        Assert.False(items[1].GetProperty("itemreceive").GetBoolean());
        Assert.False(items[1].TryGetProperty("quantity", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Snapshot_fallback_omits_backordered_fillers_but_rejects_positive_scans(bool positiveScan)
    {
        SetFlag(NetSuiteApiClientService.FailClosedRevalidationFlagVariable, "false");
        _http.FreshStatus = HttpStatusCode.BadRequest;
        var backordered = Line(2, false);
        backordered.LineQuantityBackOrdered = backordered.LineQuantity;
        backordered.ScannedQuantity = positiveScan ? 1 : 0;

        if (positiveScan)
        {
            var error = await Assert.ThrowsAsync<Exception>(() =>
                _service.SaveTOItemFulfillment([Line(5, false), backordered]));
            Assert.Contains("fully backordered", error.Message);
            Assert.Equal(0, _http.Writes);
        }
        else
        {
            Assert.True(await _service.SaveTOItemFulfillment([Line(5, false), backordered]));
            using var payload = JsonDocument.Parse(Assert.Single(_http.PostedBodies));
            var item = Assert.Single(payload.RootElement.GetProperty("item").GetProperty("items").EnumerateArray());
            Assert.Equal(5, item.GetProperty("orderLine").GetInt32());
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Fresh_fully_backordered_positive_scan_blocks_all_writes(bool reconciliation, bool restlet)
    {
        SetFlag(NetSuiteApiClientService.ReconciliationFlagVariable, reconciliation.ToString());
        _http.FreshResponse = BackorderedOrder;
        var error = await Assert.ThrowsAsync<Exception>(() => restlet
            ? _service.SaveTOItemFulfillmentViaRestlet([Line(2, false)])
            : _service.SaveTOItemFulfillment([Line(2, false)]));

        Assert.Contains("fully backordered", error.Message);
        Assert.Equal(1, _http.Reads);
        Assert.Equal(0, _http.Writes);
    }

    private const string BackorderedOrder = """
        {"items":[
          {"lineid":2,"item":27,"itemcode":"TEST-001","linequantity":2.04,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":2.04,"isclosed":"F","uomrate":12},
          {"lineid":5,"item":49214,"itemcode":"TEST-002","linequantity":11,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":1},
          {"lineid":8,"item":49215,"itemcode":"TEST-003","linequantity":5,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":10}
        ],"totalResults":3,"hasMore":false}
        """;

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
        public string? FreshResponse { get; set; }
        public HttpStatusCode FreshStatus { get; set; } = HttpStatusCode.OK;
        public List<string> PostedBodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.Contains("suiteql", StringComparison.OrdinalIgnoreCase))
            {
                Reads++;
                return new HttpResponseMessage(FreshStatus)
                {
                    Content = new StringContent(FreshResponse ?? """
                        {"items":[
                          {"lineid":1,"item":1,"itemcode":"SKU-1","linequantity":10,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":1},
                          {"lineid":2,"item":2,"itemcode":"SKU-2","linequantity":10,"quantityshipped":0,"quantitypacked":0,"quantitybackordered":0,"isclosed":"F","uomrate":1}
                        ],"totalResults":2,"hasMore":false}
                        """, Encoding.UTF8, "application/json")
                };
            }
            Writes++;
            if (request.Content is not null)
                PostedBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
