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
        string materialCode = "ITEM")
        => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            LineQuantity = lineQuantity,
            QuantityAlreadyShipped = quantityAlreadyShipped,
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            IsClosed = isClosed,
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} NAME"
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
}
