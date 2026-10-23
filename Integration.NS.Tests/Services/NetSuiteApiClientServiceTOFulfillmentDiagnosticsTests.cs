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

        Assert.Contains("is not in the submitted set", decoded);
        Assert.Contains("postedIds=", decoded);
        Assert.Contains("MISSING ITEM", decoded);
    }

    // ---------------------------------------------------------------------
    // Name drift: NetSuite names the item from its own record, so whitespace
    // and casing can differ from WMS's MaterialName. Field report 2026-10-21
    // ("BONAKID 3/400G(1.2KG)" reported as not posted).
    // ---------------------------------------------------------------------
    [Fact]
    public void Item_named_variant_matches_when_only_whitespace_differs()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(271, materialCode: "00022317", materialName: "BONAKID 3/400G (1.2KG)")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Please configure the inventory detail for BONAKID 3/400G(1.2KG).", lines);

        Assert.Contains("WMSLine 271", decoded);
        Assert.DoesNotContain("not matched", decoded);
    }

    // ---------------------------------------------------------------------
    // No exact match: the decoder must still narrow the offender without
    // server logs, using the reported base name (before the parenthesis).
    // ---------------------------------------------------------------------
    [Fact]
    public void Unmatched_item_reports_candidates_by_base_name()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(10, materialName: "YOUNG'S TOWN RED IN T/SAUCE 155G"),
            Line(271, materialCode: "00022317", materialName: "BONAKID 3/400G 1.2KG"),
            Line(272, materialCode: "00022318", materialName: "BONAKID 3/400G 2.4KG"),
            Line(999, materialName: "UNRELATED")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Please configure the inventory detail for BONAKID 3/400G (1.2KG) MILK.", lines);

        Assert.Contains("is not in the submitted set", decoded);
        Assert.Contains("candidates=[", decoded);
        Assert.Contains("WMS line 271", decoded);
        Assert.Contains("00022317", decoded);
    }

    [Fact]
    public void Unmatched_item_without_candidates_reports_the_posted_count()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(1, materialName: "YOUNG'S TOWN RED IN T/SAUCE 155G"),
            Line(2, materialName: "NOVA CHEDDAR 38G/40G")
        };

        var decoded = NetSuiteApiClientService.DecodeTOItemFulfillmentLine(
            "Please configure the inventory detail for BONAKID 3/400G(1.2KG).", lines);

        Assert.Contains("is not in the submitted set", decoded);
        Assert.Contains("none among 2 posted entries", decoded);
    }

    [Fact]
    public void Returns_empty_when_no_marker_is_present()
    {
        var lines = new List<PostTransferOrderDTO> { Line(1) };

        Assert.Equal(string.Empty, NetSuiteApiClientService.DecodeTOItemFulfillmentLine("Some other NetSuite error", lines));
    }
}
