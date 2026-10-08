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
    }

    // NetSuite rejects the correlated form with 400 [INVALID_PARAMETER]
    // "Invalid or unsupported search", so the subsidiary must be resolved inside the
    // derived table rather than in the join predicate. See SuiteQLFragments.
    [Fact]
    public async Task Query_scopes_preferred_vendor_inside_the_derived_table()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", 5);

        Assert.Contains("iv.subsidiary = (SELECT l.subsidiary FROM location l WHERE l.id = 5)", client.LastQuery);
        Assert.DoesNotContain("pv.subsidiary = loc.subsidiary", client.LastQuery);
        Assert.Contains("ON pv.item = item.id ", client.LastQuery);
    }

    // A location in the WHERE clause discards LEFT JOINed rows, which reports valid
    // barcodes as not found. Receiving supplies no location and must still resolve.
    [Fact]
    public async Task Without_a_location_the_query_never_filters_or_joins_on_location()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", null);

        Assert.DoesNotContain("loc.id =", client.LastQuery);
        Assert.DoesNotContain("location loc", client.LastQuery);
        Assert.DoesNotContain("ail.location =", client.LastQuery);
    }

    [Fact]
    public async Task Without_a_location_quantities_are_summed_so_one_row_is_returned()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", null);

        Assert.Contains("SUM(ail.quantityonhand)", client.LastQuery);
        Assert.Contains("SUM(ail.quantityavailable)", client.LastQuery);
    }

    // Unscoped, a preferred bin or vendor would come from an arbitrary location or
    // subsidiary. Omitting them beats guessing.
    [Fact]
    public async Task Without_a_location_preferred_fields_are_omitted()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", null);

        Assert.DoesNotContain("itembinquantity", client.LastQuery);
        Assert.DoesNotContain("itemvendor", client.LastQuery);
    }

    [Fact]
    public async Task With_a_location_it_is_applied_in_the_join_not_the_where_clause()
    {
        var (sut, client) = Create();

        await sut.GetBarcodeData("BC-1", 5);

        Assert.Contains("ail.item = item.id AND ail.location = 5", client.LastQuery);
        Assert.DoesNotContain("WHERE b.name = 'BC-1' AND loc.id", client.LastQuery);
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
