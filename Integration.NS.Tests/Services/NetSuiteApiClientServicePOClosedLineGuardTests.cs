using Application.DataTransferObjects.Transactions.Receiving.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Guard for the itemReceipt transform on PURCHASE ORDERS. A line that is already
/// fully received is absent from the transform's item sublist, so posting any
/// orderLine reference for it - the scanned entry or the zero-qty filler - is an
/// invalid insert on a static sublist and NetSuite rejects the whole save with
/// "Please configure the inventory detail in line N". Field failure 2026-10-17,
/// PO-THM00100000216-TR: three receipts were already posted, every line had
/// qty == recvd, yet isclosed reported F on all of them - so the closed flag is
/// not a usable signal and the remaining open quantity is.
/// </summary>
public class NetSuiteApiClientServicePOClosedLineGuardTests
{
    private static PostPurchaseOrderDTO Line(
        int lineSequenceNumber,
        decimal lineQuantity,
        decimal lineQuantityReceived = 0,
        decimal scannedQuantity = 0,
        decimal uomRate = 1,
        bool isBad = false,
        string materialCode = "ITEM")
        => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            LineQuantity = lineQuantity,
            LineQuantityReceived = lineQuantityReceived,
            ScannedQuantity = scannedQuantity,
            UoMRate = uomRate,
            IsBad = isBad,
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} NAME"
        };

    // ---------------------------------------------------------------------
    // CP-1: a fully received line contributes nothing to the payload. It must be
    // dropped from the line set the payload builder receives, so no orderLine
    // reference for it can be emitted.
    // ---------------------------------------------------------------------
    [Fact]
    public void Fully_received_line_is_excluded_from_the_payload_lines()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(1, 24, scannedQuantity: 24),
            Line(4, 36, lineQuantityReceived: 36, scannedQuantity: 36), // fully received
            Line(18, 240, lineQuantityReceived: 240)                    // fully received, unscanned
        };

        var postable = lines.Where(x => !NetSuiteApiClientService.IsClosedPOLine(x)).ToList();

        var kept = Assert.Single(postable);
        Assert.Equal(1, kept.LineSequenceNumber);
        Assert.DoesNotContain(postable, x => x.LineSequenceNumber == 4);
        Assert.DoesNotContain(postable, x => x.LineSequenceNumber == 18);
    }

    // Regression for the 2026-10-17 PO: every line was qty == recvd while
    // isclosed reported F, so the predicate must key on remaining, not a flag.
    // (PostPurchaseOrderDTO carries no IsClosed field for exactly this reason.)
    [Fact]
    public void Fully_received_line_is_closed_regardless_of_the_isclosed_flag()
    {
        Assert.True(NetSuiteApiClientService.IsClosedPOLine(Line(4, 36, lineQuantityReceived: 36, scannedQuantity: 36)));
        Assert.True(NetSuiteApiClientService.IsClosedPOLine(Line(18, 240, lineQuantityReceived: 240)));
    }

    // A fully received line carrying a scan (stale client state) is still dropped,
    // never posted as a receipt.
    [Fact]
    public void Fully_received_line_carrying_a_scan_is_still_excluded()
    {
        var closed = Line(4, 36, lineQuantityReceived: 36, scannedQuantity: 36);

        Assert.True(NetSuiteApiClientService.IsClosedPOLine(closed));
    }

    // A partially received line stays open: remaining is what drives the limit.
    [Fact]
    public void Partially_received_line_stays_open()
    {
        var line = Line(4, lineQuantity: 200, lineQuantityReceived: 150, scannedQuantity: 10);

        Assert.False(NetSuiteApiClientService.IsClosedPOLine(line));
        Assert.Equal(50, NetSuiteApiClientService.PORemainingDisplayQuantity(line));
    }

    // ---------------------------------------------------------------------
    // CP-2: scanning beyond the remaining open quantity fails with an
    // actionable descriptor naming the WMS line, material and the ordered /
    // remaining figures.
    // ---------------------------------------------------------------------
    [Fact]
    public void Scan_beyond_remaining_quantity_is_flagged()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(536, lineQuantity: 200, lineQuantityReceived: 150, scannedQuantity: 51, materialCode: "00018051")
        };

        var offenders = NetSuiteApiClientService.FindPOScansExceedingRemaining(lines);

        var message = Assert.Single(offenders);
        Assert.Contains("WMS line 536", message);
        Assert.Contains("00018051", message);
        Assert.Contains("ordered 200", message);
        Assert.Contains("remaining 50", message);
    }

    [Fact]
    public void Scan_equal_to_remaining_quantity_is_allowed()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(536, lineQuantity: 200, lineQuantityReceived: 150, scannedQuantity: 50)
        };

        Assert.Empty(NetSuiteApiClientService.FindPOScansExceedingRemaining(lines));
    }

    // Zero-qty filler lines (unscanned open lines kept for ?replace= consistency)
    // must never be mistaken for an over-scan.
    [Fact]
    public void Unscanned_filler_line_is_not_flagged()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(700, lineQuantity: 4, lineQuantityReceived: 4, scannedQuantity: 0)
        };

        Assert.Empty(NetSuiteApiClientService.FindPOScansExceedingRemaining(lines));
    }

    // ---------------------------------------------------------------------
    // CP-4: the guard runs over the whole set before any bucket is posted, so a
    // valid BAD bucket cannot commit while the GOOD bucket is invalid.
    // ---------------------------------------------------------------------
    [Fact]
    public void Valid_bad_bucket_does_not_mask_an_over_remaining_good_line()
    {
        var data = new List<PostPurchaseOrderDTO>
        {
            Line(1, lineQuantity: 10, scannedQuantity: 4, isBad: true),                     // valid BAD
            Line(2, lineQuantity: 20, lineQuantityReceived: 5, scannedQuantity: 16)          // GOOD: 16 > 15 remaining
        };

        data = data.Where(x => !NetSuiteApiClientService.IsClosedPOLine(x)).ToList();

        var offenders = NetSuiteApiClientService.FindPOScansExceedingRemaining(data);

        var message = Assert.Single(offenders);
        Assert.Contains("WMS line 2", message);
        Assert.Contains("remaining 15", message);
    }

    // ---------------------------------------------------------------------
    // CP-5: a partially received line (150 of 200 already received) stays open
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
        var line = Line(536, lineQuantity: 200, lineQuantityReceived: 150, scannedQuantity: scanned);

        var offenders = NetSuiteApiClientService.FindPOScansExceedingRemaining([line]);

        Assert.Equal(shouldFlag, offenders.Count == 1);
    }

    // ---------------------------------------------------------------------
    // CP-6: remaining is reported in the operator's display UoM - ScannedQuantity
    // is in UoM, the PO quantities are in the item's stock UoM.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(20, false)]
    [InlineData(21, true)]
    public void Remaining_is_scaled_by_the_uom_rate(decimal scanned, bool shouldFlag)
    {
        // 240 stock units at rate 12 = 20 display units remaining.
        var line = Line(18, lineQuantity: 240, lineQuantityReceived: 0, scannedQuantity: scanned, uomRate: 12);

        var offenders = NetSuiteApiClientService.FindPOScansExceedingRemaining([line]);

        Assert.Equal(shouldFlag, offenders.Count == 1);
        Assert.Equal(20, NetSuiteApiClientService.PORemainingDisplayQuantity(line));
    }

    // ---------------------------------------------------------------------
    // CP-7: the existing helpers preserve their behaviour on an ordinary open
    // order (no fully received lines, no partial receipt).
    // ---------------------------------------------------------------------
    [Fact]
    public void Open_lines_pass_through_the_guard_untouched()
    {
        var data = new List<PostPurchaseOrderDTO>
        {
            Line(1, lineQuantity: 10, lineQuantityReceived: 0, scannedQuantity: 3),
            Line(2, lineQuantity: 4, lineQuantityReceived: 0, scannedQuantity: 0)
        };

        var postable = data.Where(x => !NetSuiteApiClientService.IsClosedPOLine(x)).ToList();

        Assert.Equal(data.Count, postable.Count);
        Assert.Empty(NetSuiteApiClientService.FindPOScansExceedingRemaining(postable));
    }
}
