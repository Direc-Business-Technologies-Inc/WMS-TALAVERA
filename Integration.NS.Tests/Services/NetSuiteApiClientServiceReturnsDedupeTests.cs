using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Line-level collapse for the returns Item Receipt. ReturnsIRPayloadDTO.CreateForItemReceipt
/// maps each row 1:1 to item[].orderline, and NetSuite rejects a body that repeats an
/// orderLine with [DUPLICATE_KEYS]. The returns line query fans one transaction line into
/// several rows when the item has multiple preferred bins at the location or the preferred
/// vendor has multiple bin-assignment records there, and the mobile used to resend a partial
/// save that had already been posted. The collapse must keep one row per line per bucket,
/// keep the legitimate good+missing pair of a partially scanned line (they post as separate
/// documents), keep the bin-carrying copy, and refuse to guess when copies disagree on
/// quantity.
/// </summary>
public class NetSuiteApiClientServiceReturnsDedupeTests
{
    private static PostReturnsDTO Line(
        int lineSequenceNumber,
        decimal scannedQuantity,
        bool isMissing = false,
        int vendorBin = 0,
        int preferredBin = 0,
        decimal lineQuantity = 10m,
        decimal lineReceived = 0m,
        decimal lineBackOrdered = 0m,
        decimal uoMRate = 1m) => new()
        {
            LineSequenceNumber = lineSequenceNumber,
            ScannedQuantity = scannedQuantity,
            IsMissing = isMissing,
            NetsuiteMaterialVendorAssignedBin = vendorBin,
            NetsuiteMaterialPrefferedBinId = preferredBin,
            MaterialCode = $"MAT-{lineSequenceNumber}",
            MaterialName = $"Material {lineSequenceNumber}",
            LineQuantity = lineQuantity,
            LineQuantityReceived = lineReceived,
            LineQuantityBackOrdered = lineBackOrdered,
            UoMRate = uoMRate,
        };

    [Fact]
    public void Fanout_copies_collapse_to_one_row()
    {
        var rows = new List<PostReturnsDTO>
        {
            Line(120, 0, preferredBin: 21),
            Line(234, 7, vendorBin: 8),
            Line(234, 7, vendorBin: 8),
            Line(480, 0)
        };

        var result = NetSuiteApiClientService.CollapseReturnLines(rows);

        Assert.Equal(3, result.Count);
        Assert.Single(result, x => x.LineSequenceNumber == 234);
    }

    [Fact]
    public void Copies_with_diverging_quantities_are_rejected()
    {
        var rows = new List<PostReturnsDTO>
        {
            Line(234, 7),
            Line(234, 5)
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => NetSuiteApiClientService.CollapseReturnLines(rows));

        Assert.Contains("234", ex.Message);
        Assert.Contains("diverging quantities", ex.Message);
    }

    [Fact]
    public void Bin_carrying_copy_wins_over_a_zero_qty_filler()
    {
        var rows = new List<PostReturnsDTO>
        {
            Line(234, 0, preferredBin: 9),
            Line(234, 7, vendorBin: 8)
        };

        var result = NetSuiteApiClientService.CollapseReturnLines(rows);

        var line = Assert.Single(result);
        Assert.Equal(7, line.ScannedQuantity);
        Assert.Equal(8, line.NetsuiteMaterialVendorAssignedBin);
    }

    [Fact]
    public void Good_and_missing_pair_survives_in_separate_buckets()
    {
        var rows = new List<PostReturnsDTO>
        {
            Line(249, 4),
            Line(249, 6, isMissing: true)
        };

        var good = NetSuiteApiClientService.CollapseReturnLines(rows.Where(x => !x.IsMissing).ToList());
        var missing = NetSuiteApiClientService.CollapseReturnLines(rows.Where(x => x.IsMissing).ToList());

        Assert.Equal(4, Assert.Single(good).ScannedQuantity);
        Assert.Equal(6, Assert.Single(missing).ScannedQuantity);
        Assert.True(missing[0].IsMissing);
    }

    [Fact]
    public void Collapsed_bucket_produces_unique_order_lines_in_the_payload()
    {
        var rows = new List<PostReturnsDTO>
        {
            Line(120, 0),
            Line(234, 7, vendorBin: 8),
            Line(234, 7, vendorBin: 8),
            Line(480, 3)
        };

        var collapsed = NetSuiteApiClientService.CollapseReturnLines(rows.Where(x => !x.IsMissing).ToList());
        var payload = ReturnsIRPayloadDTO.CreateForItemReceipt(collapsed, 1, 100, 5, "test");

        var items = payload.Item.Items;
        Assert.Equal(items.Count, items.Select(x => x.OrderLine).Distinct().Count());
    }

    [Fact]
    public void Scanned_plus_missing_within_remaining_passes()
    {
        var good = new List<PostReturnsDTO> { Line(249, 1, lineQuantity: 3) };
        var missing = new List<PostReturnsDTO> { Line(249, 2, isMissing: true, lineQuantity: 3) };

        var offenders = NetSuiteApiClientService.FindReturnQuantityOverflows(good, missing);

        Assert.Empty(offenders);
    }

    [Fact]
    public void Scanned_plus_missing_beyond_remaining_is_rejected()
    {
        var good = new List<PostReturnsDTO> { Line(249, 1, lineQuantity: 3) };
        var missing = new List<PostReturnsDTO> { Line(249, 3, isMissing: true, lineQuantity: 3) };

        var offenders = NetSuiteApiClientService.FindReturnQuantityOverflows(good, missing);

        var offender = Assert.Single(offenders);
        Assert.Contains("249", offender);
        Assert.Contains("remaining 3", offender);
    }

    [Fact]
    public void Fully_scanned_line_with_a_missing_row_is_rejected()
    {
        var good = new List<PostReturnsDTO> { Line(300, 2, lineQuantity: 2) };
        var missing = new List<PostReturnsDTO> { Line(300, 1, isMissing: true, lineQuantity: 2) };

        var offenders = NetSuiteApiClientService.FindReturnQuantityOverflows(good, missing);

        Assert.Single(offenders);
    }

    [Fact]
    public void Remaining_is_derived_through_the_uo_m_rate()
    {
        // (10 - (4 + 0)) / 2 = 3 base units still open.
        var good = new List<PostReturnsDTO> { Line(400, 3, lineQuantity: 10, lineReceived: 4, uoMRate: 2) };
        var missing = new List<PostReturnsDTO> { Line(400, 1, isMissing: true, lineQuantity: 10, lineReceived: 4, uoMRate: 2) };

        var offenders = NetSuiteApiClientService.FindReturnQuantityOverflows(good, missing);

        var offender = Assert.Single(offenders);
        Assert.Contains("remaining 3", offender);
    }

    [Fact]
    public void A_missing_uo_m_rate_blocks_the_check()
    {
        var good = new List<PostReturnsDTO> { Line(500, 1, uoMRate: 0) };

        var offenders = NetSuiteApiClientService.FindReturnQuantityOverflows(good, []);

        var offender = Assert.Single(offenders);
        Assert.Contains("UoM rate", offender);
    }
}
