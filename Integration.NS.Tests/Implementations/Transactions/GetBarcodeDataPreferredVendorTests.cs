using Integration.NS.DataTransferObjects.Receiving;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

/// <summary>
/// Bugfix 2026-10-08: barcode-created STR/RTS lines must default their
/// Preferred Vendor from the item vendor master, scoped to the scanned
/// location's subsidiary, exactly like the item-picker path.
/// </summary>
public class GetBarcodeDataPreferredVendorTests
{
    private static (ReceivingIntegration Sut, CapturingNetSuiteClient Client) Create()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);
        return (sut, client);
    }

    private static BarcodeNSDTO SampleBarcode() => new()
    {
        Barcode = "BC-1",
        ItemId = 7,
        ItemName = "Widget",
        ItemCode = "W-1",
        UoMId = 3,
        UoMName = "Each",
        UoMRate = 1m,
        PreferredBin = "A-01",
        PreferredVendorId = 42,
        PreferredVendorName = "Acme Supplies",
    };

    // P1 moments: the preferred vendor is joined/scoped and selected.
    [Fact]
    public async Task Query_joins_preferred_vendor_scoped_to_location_subsidiary()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", 5);

        Assert.NotNull(client.LastQuery);
        Assert.Contains("itemvendor", client.LastQuery);
        Assert.Contains("pv.vendor", client.LastQuery);
        Assert.Contains("BUILTIN.DF(pv.vendor)", client.LastQuery);
        Assert.Contains("pv.subsidiary = loc.subsidiary", client.LastQuery);
    }

    [Fact]
    public async Task Result_maps_preferred_vendor_into_item()
    {
        var (sut, client) = Create();
        client.NextResult = SampleBarcode();

        var result = await sut.GetBarcodeData("BC-1", 5);

        Assert.NotNull(result);
        Assert.Equal(42, result!.Item!.PreferredVendorId);
        Assert.Equal("Acme Supplies", result.Item.PreferredVendorName);
    }

    [Fact]
    public async Task Result_preserves_preferred_bin()
    {
        var (sut, client) = Create();
        client.NextResult = SampleBarcode();

        var result = await sut.GetBarcodeData("BC-1", 5);

        Assert.Equal("A-01", result!.Item!.PreferredBin);
    }

    // P2: no master vendor in the subsidiary => fields stay null.
    [Fact]
    public async Task No_preferred_vendor_leaves_item_vendor_null()
    {
        var (sut, client) = Create();
        var barcode = SampleBarcode();
        barcode.PreferredVendorId = null;
        barcode.PreferredVendorName = null;
        client.NextResult = barcode;

        var result = await sut.GetBarcodeData("BC-1", 5);

        Assert.NotNull(result);
        Assert.Null(result!.Item!.PreferredVendorId);
        Assert.Null(result.Item.PreferredVendorName);
    }

    [Fact]
    public async Task Unknown_barcode_returns_null_without_throwing()
    {
        var (sut, client) = Create();
        client.NextResult = null;

        var result = await sut.GetBarcodeData("NOPE", 5);

        Assert.Null(result);
    }
}
