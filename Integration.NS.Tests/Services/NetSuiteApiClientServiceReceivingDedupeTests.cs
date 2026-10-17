using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Line-level dedupe for the TO receiving transform. The receiving line query
/// (NS_TO_Get_Itemfulfillment_Items.sql) fans one transaction line into several
/// rows when the item has multiple preferred bins at the location or the
/// preferred vendor has multiple bin-assignment records there; the mobile posts
/// every row back and TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet
/// maps each row 1:1 into lines[], so the duplicate orderLine breaks the restlet
/// save. The dedupe must collapse each (orderLine, quality, missing) triple to
/// one row while keeping the legitimate good+missing pair of a partially-scanned
/// line, the good/bad split of a mis-picked line, and the original list order.
/// </summary>
public class NetSuiteApiClientServiceReceivingDedupeTests
{
    private sealed record Line(int LineSequenceNumber, bool IsBad, bool IsMissing, decimal ScannedQuantity, int BinId);

    private static List<Line> Dedupe(List<Line> lines) =>
        NetSuiteApiClientService.DeduplicateReceivingLines(
            lines, x => x.LineSequenceNumber, x => x.IsBad, x => x.IsMissing, x => x.ScannedQuantity);

    // Production repro: the line fans out into a scanned row and a zero-qty
    // filler row (different preferred bins). The scanned row must win so its
    // bin and quantity are the ones posted.
    [Fact]
    public void Fanout_scanned_row_wins_over_filler_copy()
    {
        var lines = new List<Line>
        {
            new(100, false, false, 0, 21),
            new(234, false, false, 7, 8),   // scanned copy (bin 8 holds the stock)
            new(234, false, false, 0, 9),   // fan-out filler copy
            new(480, false, false, 0, 0)
        };

        var result = Dedupe(lines);

        Assert.Equal(3, result.Count);
        var line234 = Assert.Single(result, x => x.LineSequenceNumber == 234);
        Assert.Equal(7, line234.ScannedQuantity);
        Assert.Equal(8, line234.BinId);
    }

    // The critical receiving case: a partially-scanned line legitimately yields a
    // good entry AND a missing entry with the SAME orderLine and the SAME IsBad.
    // They belong to separate good/missing buckets and must both survive — keying
    // on (line, isBad) alone, like the packing dedupe, would silently drop one.
    [Fact]
    public void Good_and_missing_pair_for_one_line_is_kept()
    {
        var lines = new List<Line>
        {
            new(249, false, false, 4, 8),   // good: the scanned part
            new(249, false, true, 6, 8)     // missing: the unscanned remainder
        };

        var result = Dedupe(lines);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x is { IsMissing: false, ScannedQuantity: 4 });
        Assert.Contains(result, x => x is { IsMissing: true, ScannedQuantity: 6 });
    }

    [Fact]
    public void Good_bad_split_of_one_line_is_kept()
    {
        var lines = new List<Line>
        {
            new(381, false, false, 5, 8),
            new(381, true, false, 3, 8)
        };

        var result = Dedupe(lines);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x is { IsBad: false, ScannedQuantity: 5 });
        Assert.Contains(result, x => x is { IsBad: true, ScannedQuantity: 3 });
    }

    [Fact]
    public void First_appearance_order_is_preserved()
    {
        var lines = new List<Line>
        {
            new(120, false, false, 0, 0),
            new(234, false, false, 2, 5),
            new(249, false, false, 0, 0),
            new(234, false, false, 0, 6)    // fan-out filler for the second line
        };

        var result = Dedupe(lines);

        Assert.Equal(
            new[] { 120, 234, 249 },
            result.Select(x => x.LineSequenceNumber).ToArray());
        Assert.Equal(2, result[1].ScannedQuantity);
    }

    [Fact]
    public void Distinct_lines_pass_through_untouched()
    {
        var lines = new List<Line>
        {
            new(1, false, false, 0, 0),
            new(2, false, false, 4, 8),
            new(3, true, false, 1, 0)
        };

        var result = Dedupe(lines);

        Assert.Equal(lines, result);
    }
}
