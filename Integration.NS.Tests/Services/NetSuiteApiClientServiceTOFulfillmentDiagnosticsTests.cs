using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// The transfer-order itemFulfillment diagnostics decoder. NetSuite rejects a bin-less
/// inventoryDetail two ways: "line N of the item list" (an index into the payload's item
/// sublist) and "Please configure the inventory detail for &lt;item name&gt;." with no index -
/// the latter is the phrasing the web STR Packing Item Receipt surface produces (field report
/// 2026-10-18, DEL MONTE P/A SLICED 1.5TINS 432G). The decoder must name the offending WMS line
/// in both cases; the item-named form is resolved by MaterialName / MaterialCode, and the item
/// name may itself contain '.'.
/// </summary>
public class NetSuiteApiClientServiceTOFulfillmentDiagnosticsTests
{
    private static PostTransferOrderDTO Line(
        int lineSequenceNumber,
        decimal scannedQuantity = 0,
        bool isBad = false,
        int vendorBin = 0,
        int preferredBin = 0,
        string materialCode = "ITEM",
        string materialName = "ITEM NAME")
        => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            NetsuiteMaterialVendorAssignedBin = vendorBin,
            NetsuiteMaterialPrefferedBinId = preferredBin,
            MaterialCode = materialCode,
            MaterialName = materialName
        };

    // ---------------------------------------------------------------------
    // "line N of the item list" - index into the posted bucket, unchanged.
    // ---------------------------------------------------------------------
    [Fact]
    public void Line_index_variant_names_the_payload_row()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(10),
            Line(20, scannedQuantity: 3, materialCode: "00000967")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Bad Request(400): [USER_ERROR] Please configure inventory detail in line 2 of the item list.", lines);

        Assert.Contains("line2=", decoded);
        Assert.Contains("WMSLine 20", decoded);
        Assert.Contains("00000967", decoded);
    }

    // ---------------------------------------------------------------------
    // "inventory detail for <item>." - no index; resolved by name. The name
    // contains '.', so the capture must not stop at the first dot.
    // ---------------------------------------------------------------------
    [Fact]
    public void Item_named_variant_resolves_by_material_name_with_a_dotted_name()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(561, scannedQuantity: 0, materialCode: "00018051", materialName: "DEL MONTE P/A SLICED 1.5TINS 432G"),
            Line(562, scannedQuantity: 0, materialCode: "00018052", materialName: "OTHER ITEM")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Bad Request(400): [USER_ERROR] Error while accessing a resource. Please configure the inventory detail for DEL MONTE P/A SLICED 1.5TINS 432G.", lines);

        Assert.Contains("item=00018051", decoded);
        Assert.Contains("WMSLine 561", decoded);
        Assert.Contains("DEL MONTE P/A SLICED 1.5TINS 432G", decoded);
    }

    [Fact]
    public void Item_named_variant_resolves_by_material_code_too()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(561, materialCode: "00018051", materialName: "DEL MONTE P/A SLICED 1.5TINS 432G")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Please configure the inventory detail for 00018051.", lines);

        Assert.Contains("WMSLine 561", decoded);
    }

    [Fact]
    public void Item_named_variant_reports_when_no_line_matches()
    {
        var lines = new List<PostTransferOrderDTO> { Line(1, materialName: "SOMETHING ELSE") };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Please configure the inventory detail for MISSING ITEM.", lines);

        Assert.Contains("not matched to a posted line", decoded);
        Assert.Contains("MISSING ITEM", decoded);
    }

    [Fact]
    public void Returns_empty_when_no_marker_is_present()
    {
        var lines = new List<PostTransferOrderDTO> { Line(1) };

        Assert.Equal(string.Empty, NetSuiteApiClientService.DecodeTOItemFulfillmentLine("Some other NetSuite error", lines));
    }
}
