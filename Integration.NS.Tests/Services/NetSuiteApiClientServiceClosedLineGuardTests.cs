using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Guard for the itemFulfillment transform on PARTIALLY FULFILLED transfer
/// orders. A line that was already shipped (or is closed outright) is absent
/// from the transform's item sublist, so posting any orderLine reference for it
/// - the scanned entry or the zero-qty filler - is an invalid insert on a static
/// sublist and NetSuite rejects the whole save. Field failure 2026-10-16,
/// ISTR-THM00100000631 ("Pending Receipt / Partially Fulfilled"): the payload
/// carried every line, including ones no longer on the sublist, and the GOOD
/// bucket bucketed against quantitypacked instead of the remaining open qty.
/// </summary>
public class NetSuiteApiClientServiceClosedLineGuardTests
{
    private static PostTransferOrderDTO Line(
        int lineSequenceNumber,
        decimal lineQuantity,
        decimal quantityAlreadyShipped = 0,
        decimal scannedQuantity = 0,
        bool isBad = false,
        string isClosed = "F",
        string materialCode = "ITEM",
        decimal uomRate = 0,
        decimal lineQuantityPacked = 0,
        decimal lineQuantityBackOrdered = 0)
        => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            LineQuantity = lineQuantity,
            QuantityAlreadyShipped = quantityAlreadyShipped,
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            IsClosed = isClosed,
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} NAME",
            UoMRate = uomRate,
            LineQuantityPacked = lineQuantityPacked,
            LineQuantityBackOrdered = lineQuantityBackOrdered
        };

    // ---------------------------------------------------------------------
    // CP-1: a closed line contributes nothing to the payload. It must be
    // dropped from the line set the payload builder receives, so no orderLine
    // reference for it can be emitted.
    // ---------------------------------------------------------------------
    [Fact]
    public void Closed_line_is_excluded_from_the_payload_lines()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(100, 10, scannedQuantity: 10),
            Line(200, 5, isClosed: "T"),            // closed outright
            Line(300, 8, quantityAlreadyShipped: 8) // fully shipped -> remaining 0
        };

        var postable = lines.Where(x => !NetSuiteApiClientService.IsClosedFulfillmentLine(x)).ToList();

        var kept = Assert.Single(postable);
        Assert.Equal(100, kept.LineSequenceNumber);
        Assert.DoesNotContain(postable, x => x.LineSequenceNumber == 200);
        Assert.DoesNotContain(postable, x => x.LineSequenceNumber == 300);
    }

    // A closed line with a scanned entry (stale client state) is still dropped,
    // never posted as a scan.
    [Fact]
    public void Closed_line_carrying_a_scan_is_still_excluded()
    {
        var closed = Line(200, 5, scannedQuantity: 5, isClosed: "T");

        Assert.True(NetSuiteApiClientService.IsClosedFulfillmentLine(closed));
    }

    // ---------------------------------------------------------------------
    // CP-2: scanning beyond the remaining open quantity fails with an
    // actionable descriptor naming the WMS line, material and the ordered /
    // remaining figures.
    // ---------------------------------------------------------------------
    [Fact]
    public void Scan_beyond_remaining_quantity_is_flagged()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(536, lineQuantity: 200, quantityAlreadyShipped: 150, scannedQuantity: 51, materialCode: "00018051")
        };

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining(lines);

        var message = Assert.Single(offenders);
        Assert.Contains("WMS line 536", message);
        Assert.Contains("00018051", message);
        Assert.Contains("ordered 200", message);
        Assert.Contains("remaining 50", message);
    }

    [Fact]
    public void Scan_equal_to_remaining_quantity_is_allowed()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(536, lineQuantity: 200, quantityAlreadyShipped: 150, scannedQuantity: 50)
        };

        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining(lines));
    }

    // Zero-qty filler lines (unscanned open lines kept for ?replace= consistency)
    // must never be mistaken for an over-scan.
    [Fact]
    public void Unscanned_filler_line_is_not_flagged()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(700, lineQuantity: 4, quantityAlreadyShipped: 4, scannedQuantity: 0)
        };

        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining(lines));
    }

    // ---------------------------------------------------------------------
    // CP-3: the guard runs over the whole (deduped, closed-stripped) set before
    // any bucket is posted, so a valid BAD bucket cannot commit while the GOOD
    // bucket is invalid. Both buckets are validated up front; the offender is
    // identified regardless of which bucket it belongs to.
    // ---------------------------------------------------------------------
    [Fact]
    public void Valid_bad_bucket_does_not_mask_an_over_remaining_good_line()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 10, scannedQuantity: 4, isBad: true),                       // valid BAD
            Line(200, lineQuantity: 20, quantityAlreadyShipped: 5, scannedQuantity: 16)          // GOOD: 16 > 15 remaining
        };

        data = NetSuiteApiClientService.DeduplicateFulfillmentLines(
            data, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);
        data = data.Where(x => !NetSuiteApiClientService.IsClosedFulfillmentLine(x)).ToList();

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining(data);

        var message = Assert.Single(offenders);
        Assert.Contains("WMS line 200", message);
        Assert.Contains("remaining 15", message);
    }

    // ---------------------------------------------------------------------
    // CP-5: a partially shipped line (150 of 200 already shipped) stays open
    // with remaining 50; a scan of 50 passes, 51 fails.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(0, false)]
    [InlineData(49, false)]
    [InlineData(50, false)]
    [InlineData(51, true)]
    [InlineData(120, true)]
    public void Partial_line_remaining_drives_the_limit(decimal scanned, bool shouldFlag)
    {
        var line = Line(536, lineQuantity: 200, quantityAlreadyShipped: 150, scannedQuantity: scanned);

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining([line]);

        Assert.Equal(shouldFlag, offenders.Count == 1);
    }

    // ---------------------------------------------------------------------
    // CP-6: the existing dedupe/closed-line helpers preserve their behaviour on
    // an ordinary open order (no closed lines, no partial fulfillment).
    // ---------------------------------------------------------------------
    [Fact]
    public void Open_lines_pass_through_the_guard_untouched()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 10, quantityAlreadyShipped: 0, scannedQuantity: 3),
            Line(200, lineQuantity: 4, quantityAlreadyShipped: 0, scannedQuantity: 0)
        };

        var postable = data.Where(x => !NetSuiteApiClientService.IsClosedFulfillmentLine(x)).ToList();

        Assert.Equal(data.Count, postable.Count);
        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining(postable));
    }

    // ---------------------------------------------------------------------
    // The 2026-10-23 over-scan report (STR-THM00100000867, item 00032678):
    // transactionline.quantity is 888 EACHES, the line UoM is BOX12 (rate 12),
    // and ScannedQuantity accumulates in BOXES. The old guard compared boxes
    // against eaches, so the ceiling was 888 scans instead of 74.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(74, false)] // the full 74 boxes: allowed
    [InlineData(75, true)]  // one box over: flagged
    [InlineData(888, true)] // the old guard allowed this (888 <= 888 in eaches)
    public void Multi_unit_line_cap_is_the_remaining_quantity_in_display_units(decimal scanned, bool shouldFlag)
    {
        var line = Line(536, lineQuantity: 888, uomRate: 12, scannedQuantity: scanned, materialCode: "00032678");

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining([line]);

        Assert.Equal(shouldFlag, offenders.Count == 1);
    }

    [Fact]
    public void Fully_backordered_line_refuses_every_scan()
    {
        // SuiteQL on STR-THM00100000867 line 2: quantity 888, backordered 888,
        // packed 0, shipped 0. The white box showed 0 - the guard must agree.
        var line = Line(536, lineQuantity: 888, uomRate: 12, lineQuantityBackOrdered: 888, scannedQuantity: 1);

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining([line]);

        var message = Assert.Single(offenders);
        Assert.Contains("remaining 0", message);
    }

    [Fact]
    public void Packed_quantity_counts_toward_the_cap()
    {
        // 888 eaches ordered, 444 (37 boxes) already packed -> 37 boxes remain.
        var within = Line(536, lineQuantity: 888, uomRate: 12, lineQuantityPacked: 444, scannedQuantity: 37);
        var over = Line(536, lineQuantity: 888, uomRate: 12, lineQuantityPacked: 444, scannedQuantity: 38);

        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining([within]));
        Assert.Single(NetSuiteApiClientService.FindScansExceedingRemaining([over]));
    }

    [Fact]
    public void Shipped_quantity_counts_even_when_packed_is_stale()
    {
        // The device screenshot case: 276 eaches (23 boxes) planned, fully
        // shipped, quantitypacked still 0 on the row. The old max(packed, ...)
        // omission would have re-opened the line; the operator scanned 29 boxes.
        var line = Line(536, lineQuantity: 276, uomRate: 12, quantityAlreadyShipped: 276, scannedQuantity: 29);

        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining([line]);

        var message = Assert.Single(offenders);
        Assert.Contains("remaining 0", message);
    }

    [Fact]
    public void Missing_uom_rate_falls_back_to_one_to_one()
    {
        // Legacy payloads without a rate keep the previous 1:1 behaviour.
        var line = Line(536, lineQuantity: 200, quantityAlreadyShipped: 150, scannedQuantity: 51);

        Assert.Single(NetSuiteApiClientService.FindScansExceedingRemaining([line]));
    }

    // ---------------------------------------------------------------------
    // Good and bad scans of the SAME line live in different payload rows; the
    // per-row check passes each individually while their sum exceeds the cap.
    // ---------------------------------------------------------------------
    [Fact]
    public void Good_and_bad_scans_of_one_line_are_checked_as_a_total()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 20),
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 5, isBad: true)
        };

        var offenders = NetSuiteApiClientService.FindLinesExceedingRemainingAcrossBuckets(data);

        var message = Assert.Single(offenders);
        Assert.Contains("WMS line 100", message);
        Assert.Contains("scanned 25", message);
        Assert.Contains("BAD 5", message);
        Assert.Contains("remaining 23", message);
    }

    [Fact]
    public void Good_and_bad_scans_within_the_cap_pass_the_aggregate_check()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 20),
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 3, isBad: true)
        };

        Assert.Empty(NetSuiteApiClientService.FindLinesExceedingRemainingAcrossBuckets(data));
    }

    [Fact]
    public void Save_gate_throws_when_any_line_is_over_scanned()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 29, materialCode: "00018051")
        };

        var ex = Assert.Throws<Exception>(
            () => NetSuiteApiClientService.EnsureScansWithinPackableQuantity(data));

        Assert.Contains("00018051", ex.Message);
        Assert.Contains("remaining 23", ex.Message);
    }

    [Fact]
    public void Save_gate_passes_a_valid_payload()
    {
        var data = new List<PostTransferOrderDTO>
        {
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 20),
            Line(100, lineQuantity: 276, uomRate: 12, scannedQuantity: 3, isBad: true),
            Line(200, lineQuantity: 888, uomRate: 12, lineQuantityBackOrdered: 888) // filler, fully backordered
        };

        NetSuiteApiClientService.EnsureScansWithinPackableQuantity(data);
    }
}
