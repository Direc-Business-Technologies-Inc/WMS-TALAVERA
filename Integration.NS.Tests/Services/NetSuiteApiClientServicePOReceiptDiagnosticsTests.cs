using Application.DataTransferObjects.Transactions.Receiving.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// The PO item-receipt failure diagnostics added after the 2026-10-17 field reports. Two parts:
/// (1) <see cref="NetSuiteApiClientService.DecodeItemReceiptLine"/> turns NetSuite's opaque
/// "line N of the item list" into the offending WMS line so the operator/field engineer does not
/// need a server-logs round trip; it MUST index the same sublist the payload builder emits -
/// grouped by LineSequenceNumber in first-appearance order, so unscanned open lines (fillers)
/// occupy positions too. (2) the pre-flight missing-bin guard uses
/// <see cref="NetSuiteApiClientService.RequiresBin"/> and
/// <see cref="NetSuiteApiClientService.FindPOMissingBinLines"/> to fail fast when a bin-tracked
/// location resolves no bin, instead of surfacing the same cryptic 400 after the post; it judges
/// each line on its own location flag and each good/bad bucket separately.
/// </summary>
public class NetSuiteApiClientServicePOReceiptDiagnosticsTests
{
    private static PostPurchaseOrderDTO Line(
        int lineSequenceNumber,
        decimal scannedQuantity = 0,
        bool isBad = false,
        string materialCode = "ITEM",
        bool isLocationUsedBin = false,
        int vendorBinAssignmentId = 0,
        int preferredBinId = 0)
        => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            IsLocationUsedBin = isLocationUsedBin,
            VendorBinAssignmentId = vendorBinAssignmentId,
            NetsuiteMaterialPrefferedBinId = preferredBinId,
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} NAME"
        };

    private static string RejectAt(int itemListLine) =>
        $"Bad Request(400): [USER_ERROR]Error while accessing a resource. Please configure inventory detail in line {itemListLine} of the item list.";

    // ---------------------------------------------------------------------
    // Decoder — message parsing
    // ---------------------------------------------------------------------
    [Fact]
    public void Returns_empty_when_the_message_has_no_item_list_marker()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, scannedQuantity: 5) };

        Assert.Equal(string.Empty, NetSuiteApiClientService.DecodeItemReceiptLine("Some other NetSuite error", lines, isGoodPost: true));
    }

    // ---------------------------------------------------------------------
    // Decoder — index maps onto the payload sublist (fillers included)
    // ---------------------------------------------------------------------
    [Fact]
    public void Index_maps_to_the_open_line_at_that_payload_position()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(10),
            Line(20),
            Line(30, scannedQuantity: 5, materialCode: "00000967")
        };

        var decoded = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(3), lines, isGoodPost: true);

        Assert.Contains("line3=", decoded);
        Assert.Contains("WMSLine 30", decoded);
        Assert.Contains("00000967", decoded);
        Assert.Contains("qty=5", decoded);
    }

    // An unscanned open line travels as a filler, so it still occupies an item-list
    // position and must decode to that filler - not be skipped.
    [Fact]
    public void Filler_lines_occupy_payload_positions()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(10),
            Line(20),
            Line(30, scannedQuantity: 5)
        };

        var decoded = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(2), lines, isGoodPost: true);

        Assert.Contains("WMSLine 20", decoded);
        Assert.Contains("qty=0", decoded);
    }

    // A line scanned as both good and bad is one payload entry; each post decodes its
    // own quality's row.
    [Fact]
    public void Good_post_decodes_the_good_row_and_bad_post_decodes_the_bad_row()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(488, scannedQuantity: 8, isBad: false, materialCode: "A"),
            Line(488, scannedQuantity: 2, isBad: true, materialCode: "A")
        };

        var good = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(1), lines, isGoodPost: true);
        var bad = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(1), lines, isGoodPost: false);

        Assert.Contains("WMSLine 488", good);
        Assert.Contains("qty=8", good);
        Assert.Contains("qty=2", bad);
    }

    // The query can fan one line into several rows; the payload collapses them to one
    // entry, so the decoder must too — a second "line 2" cannot exist.
    [Fact]
    public void Fanned_out_duplicate_collapses_to_a_single_payload_position()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(7, scannedQuantity: 0, isLocationUsedBin: true, vendorBinAssignmentId: 0),
            Line(7, scannedQuantity: 4, isLocationUsedBin: true, vendorBinAssignmentId: 55)
        };

        var first = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(1), lines, isGoodPost: true);
        var second = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(2), lines, isGoodPost: true);

        Assert.Contains("WMSLine 7", first);
        Assert.Contains("qty=4", first);
        Assert.Contains("vendorBin=55", first);
        Assert.Equal(string.Empty, second);
    }

    [Fact]
    public void Out_of_range_index_is_ignored()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, scannedQuantity: 5) };

        Assert.Equal(string.Empty, NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(99), lines, isGoodPost: true));
    }

    // The diag reports the flags that explain a missing-bin rejection.
    [Fact]
    public void Decoded_line_reports_the_bin_flags()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(488, scannedQuantity: 3, isLocationUsedBin: true, vendorBinAssignmentId: 0, preferredBinId: 8)
        };

        var decoded = NetSuiteApiClientService.DecodeItemReceiptLine(RejectAt(1), lines, isGoodPost: true);

        Assert.Contains("usedBin=True", decoded);
        Assert.Contains("vendorBin=0", decoded);
        Assert.Contains("preferredBin=8", decoded);
    }

    // ---------------------------------------------------------------------
    // Pre-flight missing-bin guard (RequiresBin)
    // ---------------------------------------------------------------------
    [Fact]
    public void Guard_flags_a_scanned_bin_tracked_line_with_no_bin()
    {
        Assert.True(NetSuiteApiClientService.RequiresBin(isUsedBin: true, scannedQuantity: 3, vendorAssignedBin: 0, preferredBin: 0));
    }

    [Theory]
    [InlineData(false, 3, 0, 0)]    // not bin-tracked
    [InlineData(true, 0, 0, 0)]     // unscanned filler
    [InlineData(true, 3, 55, 0)]    // vendor bin resolved
    [InlineData(true, 3, 0, 8)]     // preferred bin resolved
    public void Guard_does_not_flag_other_cases(bool isUsedBin, decimal scanned, int vendorBin, int preferredBin)
    {
        Assert.False(NetSuiteApiClientService.RequiresBin(isUsedBin, scanned, vendorBin, preferredBin));
    }

    // A PO can carry lines at different locations. The guard must judge each line on its own
    // location flag: using an order-level "any line is bin-tracked" flag falsely rejects the
    // non-bin line of a mixed PO.
    [Fact]
    public void Guard_does_not_flag_a_scanned_line_at_a_non_bin_location_on_a_mixed_po()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(10, scannedQuantity: 4, isLocationUsedBin: true, vendorBinAssignmentId: 55),
            Line(20, scannedQuantity: 3, isLocationUsedBin: false)
        };

        Assert.Empty(NetSuiteApiClientService.FindPOMissingBinLines(lines));
    }

    // The payload builder judges each quality bucket separately (the good and bad posts are
    // separate NetSuite calls), so the guard must too. Here the good row has a bin and the bad
    // row does not; the bad post would still fail, so the guard must flag the line.
    [Fact]
    public void Guard_flags_the_bad_row_of_a_split_line_even_when_the_good_row_has_a_bin()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(488, scannedQuantity: 8, isBad: false, isLocationUsedBin: true, vendorBinAssignmentId: 55),
            Line(488, scannedQuantity: 2, isBad: true, isLocationUsedBin: true)
        };

        var missing = NetSuiteApiClientService.FindPOMissingBinLines(lines);

        Assert.Single(missing);
        Assert.Contains("WMS line 488", missing[0]);
    }

    // Mirror of the previous case: the good row has no bin while the bad row does.
    [Fact]
    public void Guard_flags_the_good_row_of_a_split_line_even_when_the_bad_row_has_a_bin()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(488, scannedQuantity: 8, isBad: false, isLocationUsedBin: true),
            Line(488, scannedQuantity: 2, isBad: true, isLocationUsedBin: true, vendorBinAssignmentId: 55)
        };

        var missing = NetSuiteApiClientService.FindPOMissingBinLines(lines);

        Assert.Single(missing);
        Assert.Contains("WMS line 488", missing[0]);
    }

    // A fanned-out line is judged on its scanned row, not on an unscanned duplicate that has
    // no bin yet.
    [Fact]
    public void Guard_judges_a_fanned_line_on_its_scanned_row()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(7, scannedQuantity: 0, isLocationUsedBin: true),
            Line(7, scannedQuantity: 4, isLocationUsedBin: true, vendorBinAssignmentId: 55)
        };

        Assert.Empty(NetSuiteApiClientService.FindPOMissingBinLines(lines));
    }
}
