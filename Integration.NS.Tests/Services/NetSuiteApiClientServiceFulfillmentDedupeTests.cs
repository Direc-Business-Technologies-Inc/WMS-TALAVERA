using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Line-level dedupe for the itemFulfillment transform. The packing line
/// queries fan out one transaction line into several rows when the item has
/// multiple preferred bins at the location or the preferred vendor has multiple
/// bin-assignment records there; the mobile posts every row back and NetSuite
/// then sees duplicate orderLine entries. Under
/// ?replace=item.inventoryDetail.inventoryAssignment the zero-qty filler copy's
/// EMPTY assignment collides with the scanned copy's assignment and NetSuite
/// rejects with "Please configure the inventory detail in line N of the item
/// list" naming the scanned line (field failure 2026-10-15, line 108 /
/// WMSLine 536). The dedupe must collapse each (line, quality) pair to one row
/// while keeping legitimate good/bad split scans and the original list order.
/// </summary>
public class NetSuiteApiClientServiceFulfillmentDedupeTests
{
    private sealed record Line(int LineSequenceNumber, bool IsBad, decimal ScannedQuantity, int BinId);

    private static List<Line> Dedupe(List<Line> lines) =>
        NetSuiteApiClientService.DeduplicateFulfillmentLines(
            lines, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);

    // Production repro: the line fans out into a scanned row and a zero-qty
    // filler row (different preferred bins). The scanned row must win so its
    // bin and quantity are the ones posted.
    [Fact]
    public void Fanout_scanned_row_wins_over_filler_copy()
    {
        var lines = new List<Line>
        {
            new(100, false, 0, 21),
            new(536, false, 8, 8),   // scanned copy (bin 8 holds the stock)
            new(536, false, 0, 9),   // fan-out filler copy
            new(700, false, 0, 0)
        };

        var result = Dedupe(lines);

        Assert.Equal(3, result.Count);
        var line536 = Assert.Single(result, x => x.LineSequenceNumber == 536);
        Assert.Equal(8, line536.ScannedQuantity);
        Assert.Equal(8, line536.BinId);
    }

    // Neither copy scanned (e.g. the operator never touched the line): collapse
    // to the first row deterministically so the body order is stable.
    [Fact]
    public void Fanout_unscanned_duplicates_collapse_to_first_row()
    {
        var lines = new List<Line>
        {
            new(536, false, 0, 8),
            new(536, false, 0, 9)
        };

        var result = Dedupe(lines);

        var line536 = Assert.Single(result);
        Assert.Equal(8, line536.BinId);
    }

    // A good/bad split scan of one line is legitimate input (two rows, one per
    // quality bucket) and must survive dedupe — only same-quality duplicates
    // are fan-out artifacts.
    [Fact]
    public void Good_bad_split_of_one_line_is_kept()
    {
        var lines = new List<Line>
        {
            new(536, false, 5, 8),
            new(536, true, 3, 8)
        };

        var result = Dedupe(lines);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x is { IsBad: false, ScannedQuantity: 5 });
        Assert.Contains(result, x => x is { IsBad: true, ScannedQuantity: 3 });
    }

    // The mobile Refresh() restores scanned quantity onto EVERY row sharing the
    // key, so both fan-out copies can carry the scanned qty. Dedupe must pick
    // one row, never sum — summing would over-fulfill the line.
    [Fact]
    public void Double_scanned_duplicates_are_not_summed()
    {
        var lines = new List<Line>
        {
            new(536, false, 8, 8),
            new(536, false, 8, 9)
        };

        var result = Dedupe(lines);

        var line536 = Assert.Single(result);
        Assert.Equal(8, line536.ScannedQuantity);
    }

    // GroupBy preserves first-appearance order; the posted body order feeds the
    // "line N of the item list" decoder, so positions must not shift.
    [Fact]
    public void First_appearance_order_is_preserved()
    {
        var lines = new List<Line>
        {
            new(300, false, 0, 0),
            new(100, false, 2, 5),
            new(200, false, 0, 0),
            new(100, false, 0, 6)   // fan-out filler for the second line
        };

        var result = Dedupe(lines);

        Assert.Equal(
            new[] { 300, 100, 200 },
            result.Select(x => x.LineSequenceNumber).ToArray());
        Assert.Equal(2, result[1].ScannedQuantity);
        Assert.Equal(5, result[1].BinId);
    }

    [Fact]
    public void Distinct_lines_pass_through_untouched()
    {
        var lines = new List<Line>
        {
            new(1, false, 0, 0),
            new(2, false, 4, 8),
            new(3, true, 1, 0)
        };

        var result = Dedupe(lines);

        Assert.Equal(lines, result);
    }
}
