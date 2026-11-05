using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Save-time reconciliation (resolution plan 2026-10-23, Phase 1). The server
/// owns the fulfillment line set: every submitted scan must still be an open
/// line, and every other open line is completed into the payload as a
/// zero-quantity filler. The mobile vendor-scoped read is never the authority.
/// </summary>
[Collection("Environment flags")]
public class NetSuiteApiClientServiceTOFulfillmentReconciliationTests
{
    private const int OrderId = 9001;
    private const bool UseBin = true;

    private static PostTransferOrderDTO Submitted(
        int lineId,
        decimal scannedQuantity,
        bool isBad = false,
        string materialCode = "ITEM")
        => new()
        {
            NetsuiteOrderInternalId = OrderId,
            LineSequenceNumber = lineId,
            NetsuiteMaterialInternalId = lineId * 10,
            MaterialCode = materialCode,
            MaterialName = $"{materialCode} NAME",
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            IsLocationUsedBin = UseBin
        };

    private static NetSuiteApiClientService.TOFulfillmentFreshLineState Fresh(
        int lineId,
        decimal lineQuantity,
        decimal quantityShipped = 0,
        string isClosed = "F",
        int? itemId = null,
        string? itemCode = null,
        decimal uoMRate = 1m)
        => new()
        {
            LineId = lineId,
            Item = itemId ?? lineId * 10,
            ItemCode = itemCode ?? $"ITEM-{lineId}",
            ItemName = $"ITEM-{lineId} NAME",
            LineQuantity = lineQuantity,
            QuantityShipped = quantityShipped,
            IsClosed = isClosed,
            UoMRate = uoMRate
        };

    private static NetSuiteApiClientService.TOFulfillmentReconciliation Reconcile(
        List<PostTransferOrderDTO> submitted,
        NetSuiteApiClientService.TOFulfillmentFreshLineState[] fresh)
        => NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

    [Fact]
    public void Open_lines_not_submitted_become_zero_quantity_fillers()
    {
        var submitted = new List<PostTransferOrderDTO> { Submitted(100, 4) };
        var fresh = new[] { Fresh(100, 10), Fresh(200, 7), Fresh(300, 2) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        Assert.Empty(result.RejectedScans);
        Assert.Equal(3, result.OpenLineCount);
        Assert.Equal(2, result.FillerCount);
        Assert.Equal([100, 200, 300], result.Lines.Select(x => x.LineSequenceNumber));

        Assert.Equal(4, result.Lines.Single(x => x.LineSequenceNumber == 100).ScannedQuantity);
        Assert.All(
            result.Lines.Where(x => x.LineSequenceNumber != 100),
            filler =>
            {
                Assert.Equal(0, filler.ScannedQuantity);
                Assert.False(filler.IsBad);
                Assert.Equal(OrderId, filler.NetsuiteOrderInternalId);
                Assert.True(filler.IsLocationUsedBin);
            });
    }

    [Fact]
    public void Scanned_line_that_is_no_longer_open_is_rejected_not_dropped()
    {
        // The mobile loaded the line and the operator has the goods in hand, but
        // the line was fulfilled elsewhere in the meantime. Silently dropping it
        // would strand stock without a message.
        var submitted = new List<PostTransferOrderDTO> { Submitted(200, 3, materialCode: "BONAKID") };
        var fresh = new[] { Fresh(200, lineQuantity: 5, quantityShipped: 5) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        var rejected = Assert.Single(result.RejectedScans);
        Assert.Contains("WMS line 200", rejected);
        Assert.Contains("BONAKID", rejected);
        Assert.Contains("scanned 3", rejected);
        Assert.DoesNotContain(result.Lines, x => x.ScannedQuantity > 0);
    }

    [Fact]
    public void Closed_line_is_never_part_of_the_open_set()
    {
        var submitted = new List<PostTransferOrderDTO> { Submitted(100, 2) };
        var fresh = new[] { Fresh(100, 10), Fresh(200, 10, isClosed: "T") };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        Assert.Equal(1, result.OpenLineCount);
        Assert.Equal(0, result.FillerCount);
        Assert.DoesNotContain(result.Lines, x => x.LineSequenceNumber == 200);
    }

    [Fact]
    public void Fresh_quantity_state_overwrites_the_stale_snapshot()
    {
        var submitted = new List<PostTransferOrderDTO> { Submitted(100, 6) };
        submitted[0].LineQuantity = 10;
        submitted[0].QuantityAlreadyShipped = 0;

        var fresh = new[] { Fresh(100, lineQuantity: 10, quantityShipped: 8, uoMRate: 2) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        var line = Assert.Single(result.Lines);
        Assert.Equal(10, line.LineQuantity);
        Assert.Equal(8, line.QuantityAlreadyShipped);
        Assert.Equal(2, line.RemainingQuantity);
        // The save diagnostic must report the rate the save ran on, not the
        // device's load-time snapshot.
        Assert.Equal(2, line.UoMRate);

        // The over-remaining guard now sees the fresh state and must reject the
        // scan of 6 (only 2 remain).
        Assert.Single(NetSuiteApiClientService.FindScansExceedingRemaining(result.Lines));
    }

    [Fact]
    public void Good_and_bad_scans_of_one_line_both_survive()
    {
        var submitted = new List<PostTransferOrderDTO>
        {
            Submitted(100, 3, isBad: false),
            Submitted(100, 2, isBad: true)
        };
        var fresh = new[] { Fresh(100, 10) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        Assert.Empty(result.RejectedScans);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(0, result.FillerCount);
        Assert.Single(result.Lines, x => x.IsBad);
    }

    [Fact]
    public void Stale_client_zero_quantity_filler_is_replaced_by_the_authoritative_filler()
    {
        var submitted = new List<PostTransferOrderDTO>
        {
            Submitted(100, 2),
            Submitted(200, 0) // stale mobile filler, line 200 still open
        };
        var fresh = new[] { Fresh(100, 10), Fresh(200, 5) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        Assert.Empty(result.RejectedScans);
        Assert.Equal(2, result.Lines.Count);
        Assert.Single(result.Lines, x => x.LineSequenceNumber == 200);
        Assert.Equal(1, result.FillerCount);
    }

    [Fact]
    public void Filler_carries_the_fresh_item_identity_the_mobile_never_loaded()
    {
        var submitted = new List<PostTransferOrderDTO> { Submitted(100, 1) };
        var fresh = new[]
        {
            Fresh(100, 4),
            Fresh(555, 9, itemId: 123456, itemCode: "00045638")
        };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(submitted, fresh, OrderId, UseBin);

        var filler = result.Lines.Single(x => x.LineSequenceNumber == 555);
        Assert.Equal(123456, filler.NetsuiteMaterialInternalId);
        Assert.Equal("00045638", filler.MaterialCode);
        Assert.Equal(0, filler.ScannedQuantity);
        Assert.Equal(9, filler.LineQuantity);
    }

    [Fact]
    public void No_submitted_rows_means_every_open_line_is_a_filler()
    {
        var fresh = new[] { Fresh(100, 2), Fresh(200, 3) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(
            [], fresh, OrderId, UseBin);

        Assert.Equal(2, result.Lines.Count);
        Assert.All(result.Lines, x => Assert.Equal(0, x.ScannedQuantity));
        Assert.Equal(2, result.FillerCount);
    }

    // -----------------------------------------------------------------
    // Phase 1.5 / D6 - reconciliation runs once, but each POSTED BUCKET
    // must address every open line. Addressing only the reconciled list is
    // not enough: the BAD and GOOD documents are separate transforms.
    // -----------------------------------------------------------------

    [Fact]
    public void Bad_bucket_carries_a_filler_for_every_open_line_it_does_not_scan()
    {
        var completed = Reconcile(
            [Submitted(100, 3, isBad: true), Submitted(200, 2)],
            [Fresh(100, 10), Fresh(200, 5), Fresh(300, 4)]);

        var bad = NetSuiteApiClientService.BuildTOFulfillmentBucketLines(
            completed.Lines, isBadBucket: true);

        Assert.Equal([100, 200, 300], bad.Select(x => x.LineSequenceNumber));
        Assert.Equal(3, bad.Single(x => x.LineSequenceNumber == 100).ScannedQuantity);
        Assert.All(
            bad.Where(x => x.LineSequenceNumber != 100),
            x => Assert.Equal(0, x.ScannedQuantity));

        NetSuiteApiClientService.EnsureTOFulfillmentBucketIsComplete(
            "BAD", bad, completed.Lines, isBadBucket: true);
    }

    [Fact]
    public void Good_bucket_addresses_the_bad_scanned_line_at_zero_when_it_stays_open()
    {
        var completed = Reconcile(
            [Submitted(100, 3, isBad: true), Submitted(200, 2)],
            [Fresh(100, 10), Fresh(200, 5)]);

        var good = NetSuiteApiClientService.BuildTOFulfillmentBucketLines(
            completed.Lines, isBadBucket: false);

        Assert.Equal([100, 200], good.Select(x => x.LineSequenceNumber).OrderBy(x => x));
        Assert.Equal(2, good.Single(x => x.LineSequenceNumber == 200).ScannedQuantity);
        Assert.Equal(0, good.Single(x => x.LineSequenceNumber == 100).ScannedQuantity);

        NetSuiteApiClientService.EnsureTOFulfillmentBucketIsComplete(
            "GOOD", good, completed.Lines, isBadBucket: false);
    }

    [Fact]
    public void Good_bucket_drops_the_filler_for_a_line_the_bad_post_closes()
    {
        // The BAD post runs first and ships the whole remaining 3, so the GOOD
        // transform created afterwards no longer exposes line 100. A filler
        // referencing it would be an invalid insert on a static sublist.
        var completed = Reconcile(
            [Submitted(100, 3, isBad: true), Submitted(200, 2)],
            [Fresh(100, 3), Fresh(200, 5)]);

        var good = NetSuiteApiClientService.BuildTOFulfillmentBucketLines(
            completed.Lines, isBadBucket: false);

        Assert.DoesNotContain(good, x => x.LineSequenceNumber == 100);
        NetSuiteApiClientService.EnsureTOFulfillmentBucketIsComplete(
            "GOOD", good, completed.Lines, isBadBucket: false);
    }

    [Fact]
    public void Bucket_guard_rejects_a_payload_that_leaves_an_open_line_unaddressed()
    {
        // Reproduces the pre-fix BAD payload: bad scans only, no fillers.
        var completed = Reconcile(
            [Submitted(100, 3, isBad: true)],
            [Fresh(100, 10), Fresh(200, 5)]);

        var badScansOnly = completed.Lines.Where(x => x.IsBad).ToList();

        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.EnsureTOFulfillmentBucketIsComplete(
                "BAD", badScansOnly, completed.Lines, isBadBucket: true));

        Assert.Contains("missing=[200]", exception.Message);
        Assert.Contains("Nothing was saved", exception.Message);
    }

    [Fact]
    public void Bucket_guard_rejects_a_duplicate_order_line_in_one_payload()
    {
        var completed = Reconcile(
            [Submitted(100, 3, isBad: true)],
            [Fresh(100, 10)]);

        var duplicated = completed.Lines.Concat(completed.Lines).ToList();

        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.EnsureTOFulfillmentBucketIsComplete(
                "BAD", duplicated, completed.Lines, isBadBucket: true));

        Assert.Contains("duplicate=[100]", exception.Message);
    }

    // -----------------------------------------------------------------
    // Phase 1.6 - the silent drop. Before paging, an unpaged read returned
    // only the first 1,000 rows and a scan on line 1,500 was "absent from
    // the order", i.e. classified closed and stripped with no message.
    // -----------------------------------------------------------------
    [Fact]
    public async Task Scanned_line_beyond_the_suiteql_row_cap_is_never_treated_as_closed()
    {
        const int total = 1657;

        static Task<Application.DataTransferObjects.Others.NS.NetSuiteResponse<
            NetSuiteApiClientService.TOFulfillmentFreshLineState>> Fetch(int limit, int offset)
        {
            var items = Enumerable
                .Range(offset + 1, Math.Max(0, Math.Min(limit, total - offset)))
                .Select(id => Fresh(id, 10))
                .ToList();

            return Task.FromResult(
                new Application.DataTransferObjects.Others.NS.NetSuiteResponse<
                    NetSuiteApiClientService.TOFulfillmentFreshLineState>
                {
                    items = items,
                    count = items.Count,
                    totalResults = total,
                    hasMore = offset + limit < total
                });
        }

        var fresh = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            Fetch, pageSize: 1000, maxPages: 100, context: "regression");

        var submitted = new List<PostTransferOrderDTO> { Submitted(1500, 2) };

        var result = NetSuiteApiClientService.ReconcileTOFulfillmentLines(
            submitted, fresh, OrderId, UseBin);

        Assert.Empty(result.RejectedScans);
        Assert.Equal(total, result.OpenLineCount);
        Assert.Equal(total - 1, result.FillerCount);
        Assert.Contains(result.Lines, x => x.LineSequenceNumber == 1500 && x.ScannedQuantity == 2);
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData("", true, true)]
    [InlineData("true", false, true)]
    [InlineData("1", false, true)]
    [InlineData("on", false, true)]
    [InlineData("false", true, false)]
    [InlineData("0", true, false)]
    [InlineData("off", true, false)]
    [InlineData("yes", false, true)]
    [InlineData("yes", true, true)]
    [InlineData("banana", true, true)]
    public void Flag_parsing_falls_back_to_the_default(string? value, bool defaultValue, bool expected)
    {
        Assert.Equal(expected, NetSuiteApiClientService.ReadFlag(value, defaultValue));
    }

    [Fact]
    public void Default_flags_are_reconciliation_on_and_fail_closed_off()
    {
        // Guards the deployment sequence: reconciliation ships enabled (rollback
        // is an explicit false), while fail-closed stays off for the measured
        // cycle (D5).
        var originalReconciliation = Environment.GetEnvironmentVariable(
            NetSuiteApiClientService.ReconciliationFlagVariable);
        var originalFailClosed = Environment.GetEnvironmentVariable(
            NetSuiteApiClientService.FailClosedRevalidationFlagVariable);

        try
        {
            Environment.SetEnvironmentVariable(NetSuiteApiClientService.ReconciliationFlagVariable, null);
            Environment.SetEnvironmentVariable(NetSuiteApiClientService.FailClosedRevalidationFlagVariable, null);

            Assert.True(NetSuiteApiClientService.ReconciliationEnabled);
            Assert.False(NetSuiteApiClientService.FailClosedRevalidationEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                NetSuiteApiClientService.ReconciliationFlagVariable, originalReconciliation);
            Environment.SetEnvironmentVariable(
                NetSuiteApiClientService.FailClosedRevalidationFlagVariable, originalFailClosed);
        }
    }
}

/// <summary>
/// Environment variables are process-wide. Any test that mutates them must not
/// run in parallel with the rest of the suite, or a concurrent flag read flakes.
/// </summary>
[CollectionDefinition("Environment flags", DisableParallelization = true)]
public class EnvironmentFlagCollection
{
}
