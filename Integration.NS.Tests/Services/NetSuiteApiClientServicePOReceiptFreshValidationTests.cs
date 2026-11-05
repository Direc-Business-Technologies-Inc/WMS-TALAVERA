using Application.DataTransferObjects.Transactions.Receiving.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

public class NetSuiteApiClientServicePOReceiptFreshValidationTests
{
    private static PostPurchaseOrderDTO Line(int id, decimal scanned, bool bad = false) => new()
    {
        NetsuiteOrderInternalId = 123,
        LineSequenceNumber = id,
        NetsuiteMaterialInternalId = id + 100,
        MaterialCode = $"ITEM-{id}",
        LineQuantity = 24,
        LineQuantityReceived = 0, // deliberately stale client snapshot
        UoMRate = 1,
        ScannedQuantity = scanned,
        IsBad = bad
    };

    private static NetSuiteApiClientService.POReceiptFreshLineState Fresh(
        int id, decimal ordered = 24, decimal received = 0, decimal rate = 1) => new()
    {
        LineId = id,
        Item = id + 100,
        LineQuantity = ordered,
        QuantityReceived = received,
        UoMRate = rate
    };

    [Fact]
    public void Rejects_original_scan_against_a_partially_received_line()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines([Line(8, 20)], [Fresh(8, received: 20)]));

        Assert.Contains("WMS line 8", error.Message);
        Assert.Contains("GOOD + BAD 20, remaining 4", error.Message);
    }

    [Fact]
    public void Rejects_good_and_bad_that_individually_fit_but_together_exceed_remaining()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines(
                [Line(8, 3), Line(8, 2, bad: true)], [Fresh(8, received: 20)]));

        Assert.Contains("GOOD + BAD 5, remaining 4", error.Message);
    }

    [Fact]
    public void Allows_exact_remaining_and_refreshes_quantities_while_preserving_fillers()
    {
        var good = Line(8, 3);
        var bad = Line(8, 1, bad: true);
        var filler = Line(9, 0);
        filler.UoMRate = 2;

        var lines = NetSuiteApiClientService.ValidatePOReceiptLines(
            [good, filler, bad], [Fresh(8, received: 20), Fresh(9, received: 2, rate: 2)]);

        Assert.Equal(3, lines.Count);
        Assert.Equal(20, good.LineQuantityReceived);
        Assert.Equal(2, filler.LineQuantityReceived);
        Assert.Equal(2, filler.UoMRate);
        Assert.Contains(lines, x => x.LineSequenceNumber == 9 && x.ScannedQuantity == 0);
    }

    [Fact]
    public void Rejects_missing_open_filler_or_a_scanned_closed_line()
    {
        var missing = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines([Line(8, 2)], [Fresh(8), Fresh(9)]));
        Assert.Contains("open WMS line(s) 9", missing.Message);

        var closed = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines(
                [Line(8, 1), Line(9, 2)], [Fresh(8), Fresh(9, received: 24)]));
        Assert.Contains("WMS line 9", closed.Message);
        Assert.Contains("remaining 0", closed.Message);
    }

    [Fact]
    public void Rejects_ambiguous_duplicate_quality_rows_instead_of_posting_only_one()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines(
                [Line(8, 1), Line(8, 2)], [Fresh(8)]));
        Assert.Contains("duplicate", error.Message);
    }

    [Fact]
    public void Rejects_a_changed_uom_instead_of_reinterpreting_scanned_units()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            NetSuiteApiClientService.ValidatePOReceiptLines([Line(8, 2)], [Fresh(8, rate: 2)]));
        Assert.Contains("unit of measure", error.Message);
    }
}
