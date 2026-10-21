using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Freshness revalidation for the TO itemFulfillment transform (field report
/// 2026-10-21, "invalid sublist or line item operation", diag replace=True
/// usedBin=True scannedLines=2 totalLines=714). Every posting guard runs on the
/// line snapshot the device loaded; on a large order that snapshot can be hours
/// old. A line fulfilled elsewhere or closed in the window is absent from the
/// transform's item sublist and any orderLine reference for it is an invalid
/// insert on a static sublist that fails the whole save.
/// <see cref="NetSuiteApiClientService.MergeFreshTOFulfillmentLineState"/> merges
/// freshly-read NetSuite state into the posted rows BEFORE the existing guards,
/// so the closed-line strip and the over-remaining check judge current state with
/// their logic unchanged.
/// </summary>
public class NetSuiteApiClientServiceFreshnessRevalidationTests
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

    private static NetSuiteApiClientService.TOFulfillmentFreshLineState Fresh(
        int lineId,
        decimal lineQuantity,
        decimal quantityShipped = 0,
        string isClosed = "F")
        => new()
        {
            LineId = lineId,
            LineQuantity = lineQuantity,
            QuantityShipped = quantityShipped,
            IsClosed = isClosed
        };

    // ---------------------------------------------------------------------
    // CP-1: fresh figures overwrite the snapshot. A line the device believes
    // is fully open but NetSuite says is partially shipped must carry the
    // fresh remaining quantity into the over-remaining guard.
    // ---------------------------------------------------------------------
    [Fact]
    public void Fresh_shipped_quantity_overwrites_the_snapshot()
    {
        var line = Line(100, lineQuantity: 200, quantityAlreadyShipped: 0, scannedQuantity: 51);
        var fresh = new[] { Fresh(100, lineQuantity: 200, quantityShipped: 150) };

        NetSuiteApiClientService.MergeFreshTOFulfillmentLineState([line], fresh);

        // Snapshot said remaining 200; fresh says 50 - the scan of 51 must now be
        // flagged by the existing over-remaining guard, unchanged.
        var offenders = NetSuiteApiClientService.FindScansExceedingRemaining([line]);
        Assert.Single(offenders);
        Assert.Contains("remaining 50", offenders[0]);
    }

    // ---------------------------------------------------------------------
    // CP-2: a line absent from the fresh state no longer exists on the order's
    // SHIPPING side - the condition NetSuite reports as an invalid insert on a
    // static sublist. It must be marked closed so the existing closed-line
    // strip removes it from the payload.
    // ---------------------------------------------------------------------
    [Fact]
    public void Line_missing_from_fresh_state_is_marked_closed_and_stripped()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(100, 10, scannedQuantity: 0),  // filler
            Line(200, 5, scannedQuantity: 0)    // filler fulfilled elsewhere
        };
        var fresh = new[] { Fresh(100, lineQuantity: 10) }; // line 200 gone

        NetSuiteApiClientService.MergeFreshTOFulfillmentLineState(lines, fresh);

        var postable = lines.Where(x => !NetSuiteApiClientService.IsClosedFulfillmentLine(x)).ToList();
        var kept = Assert.Single(postable);
        Assert.Equal(100, kept.LineSequenceNumber);
    }

    // ---------------------------------------------------------------------
    // CP-3: a SCANNED line that went closed or fully shipped must be reported,
    // not silently dropped - the operator physically holds the goods. The
    // descriptor names the WMS line, material, scanned and fresh figures.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData(50, "T")]   // closed outright after load
    [InlineData(50, "F")]   // fully shipped after load (remaining 0)
    public void Newly_closed_scanned_line_is_reported(decimal quantityShipped, string isClosed)
    {
        var line = Line(300, lineQuantity: 50, scannedQuantity: 20, materialCode: "BONAKID");
        var fresh = new[] { Fresh(300, lineQuantity: 50, quantityShipped: quantityShipped, isClosed: isClosed) };

        var newlyClosed = NetSuiteApiClientService.MergeFreshTOFulfillmentLineState([line], fresh);

        var message = Assert.Single(newlyClosed);
        Assert.Contains("WMS line 300", message);
        Assert.Contains("BONAKID", message);
        Assert.Contains("scanned 20", message);
    }

    // A closed line that carries NO scan (a stale filler) is routine: dropped
    // silently by the strip, never reported to the operator.
    [Fact]
    public void Newly_closed_filler_line_is_not_reported()
    {
        var line = Line(200, 5, scannedQuantity: 0);
        var fresh = new[] { Fresh(200, lineQuantity: 5, isClosed: "T") };

        var newlyClosed = NetSuiteApiClientService.MergeFreshTOFulfillmentLineState([line], fresh);

        Assert.Empty(newlyClosed);
        Assert.True(NetSuiteApiClientService.IsClosedFulfillmentLine(line));
    }

    // ---------------------------------------------------------------------
    // CP-4: the merge preserves the previously-verified guard behaviour on an
    // ordinary open order - no fresh change, no new failure mode.
    // ---------------------------------------------------------------------
    [Fact]
    public void Unchanged_open_lines_pass_through_and_stay_postable()
    {
        var lines = new List<PostTransferOrderDTO>
        {
            Line(100, 10, scannedQuantity: 4, isBad: true),
            Line(200, 20, scannedQuantity: 15)
        };
        var fresh = new[]
        {
            Fresh(100, lineQuantity: 10, quantityShipped: 0),
            Fresh(200, lineQuantity: 20, quantityShipped: 5)
        };

        var newlyClosed = NetSuiteApiClientService.MergeFreshTOFulfillmentLineState(lines, fresh);

        Assert.Empty(newlyClosed);
        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining(lines));
        Assert.All(lines, x => Assert.False(NetSuiteApiClientService.IsClosedFulfillmentLine(x)));
    }

    // A scan exactly equal to the fresh remaining is still valid - the merge
    // must not tighten the limit.
    [Fact]
    public void Scan_equal_to_fresh_remaining_is_allowed()
    {
        var line = Line(536, lineQuantity: 200, quantityAlreadyShipped: 0, scannedQuantity: 50);
        var fresh = new[] { Fresh(536, lineQuantity: 200, quantityShipped: 150) };

        NetSuiteApiClientService.MergeFreshTOFulfillmentLineState([line], fresh);

        Assert.Empty(NetSuiteApiClientService.FindScansExceedingRemaining([line]));
    }

    // A closed-in-snapshot line that fresh state says is open again must NOT
    // be reported or stripped - fresh state is the single source of truth.
    [Fact]
    public void Stale_closed_flag_is_corrected_by_fresh_open_state()
    {
        var line = Line(400, 10, scannedQuantity: 6, isClosed: "T");
        var fresh = new[] { Fresh(400, lineQuantity: 10, quantityShipped: 0, isClosed: "F") };

        var newlyClosed = NetSuiteApiClientService.MergeFreshTOFulfillmentLineState([line], fresh);

        Assert.Empty(newlyClosed);
        Assert.False(NetSuiteApiClientService.IsClosedFulfillmentLine(line));
    }
}
