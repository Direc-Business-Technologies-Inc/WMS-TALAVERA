using Application.DataTransferObjects.Others.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// The save-time authoritative read (resolution plan 2026-10-23, Phase 1.1/1.2)
/// pages SuiteQL with the limit/offset + totalResults contract. It must return
/// every line of a 1,000+ line document and must throw rather than return a
/// partially-read set: a truncated set is why a scanned line beyond NetSuite's
/// row cap used to be classified as closed and silently stripped.
/// </summary>
public class NetSuiteApiClientServiceSuiteQLPagingTests
{
    private const int PageSize = 1000;
    private const int MaxPages = 100;
    private const string Context = "TO fulfillment line read for order 9001";

    private sealed class PagingFake(int total)
    {
        public List<(int Limit, int Offset)> Calls { get; } = [];
        public int? FailOnCall { get; init; }
        public int? ShortPageOnCall { get; init; }
        public bool OmitTotalResults { get; init; }
        public bool AlwaysReturnFullPage { get; init; }

        public Task<NetSuiteResponse<int>> Fetch(int limit, int offset)
        {
            Calls.Add((limit, offset));

            if (FailOnCall == Calls.Count)
            {
                throw new InvalidOperationException("Synthetic page failure.");
            }

            var pageSize = AlwaysReturnFullPage
                ? limit
                : Math.Max(0, Math.Min(limit, total - offset));

            if (ShortPageOnCall == Calls.Count)
            {
                pageSize = Math.Min(pageSize, 1);
            }

            var items = Enumerable.Range(offset + 1, pageSize).ToList();

            return Task.FromResult(new NetSuiteResponse<int>
            {
                items = items,
                count = items.Count,
                totalResults = OmitTotalResults ? 0 : total,
                hasMore = AlwaysReturnFullPage || offset + limit < total
            });
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 1)]
    [InlineData(1001, 2)]
    [InlineData(1657, 2)]
    [InlineData(2000, 2)]
    [InlineData(2001, 3)]
    public async Task Reads_every_row_with_the_expected_call_count(int total, int expectedCalls)
    {
        var fake = new PagingFake(total);

        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            fake.Fetch, PageSize, MaxPages, Context);

        Assert.Equal(total, rows.Count);
        Assert.Equal(expectedCalls, fake.Calls.Count);
        Assert.Equal(Enumerable.Range(1, total), rows);
    }

    [Fact]
    public async Task Pages_are_requested_with_a_fixed_size_and_increasing_offsets()
    {
        var fake = new PagingFake(2001);

        await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            fake.Fetch, PageSize, MaxPages, Context);

        Assert.Equal([(1000, 0), (1000, 1000), (1000, 2000)], fake.Calls);
    }

    [Fact]
    public async Task A_short_page_that_contradicts_total_results_throws()
    {
        var fake = new PagingFake(total: 1657) { ShortPageOnCall = 2 };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(fake.Fetch, PageSize, MaxPages, Context));

        Assert.Contains("1,001 of 1,657", exception.Message);
        Assert.Contains("partial list", exception.Message);
    }

    [Fact]
    public async Task A_page_failure_fails_the_whole_read()
    {
        var fake = new PagingFake(total: 2000) { FailOnCall = 2 };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(fake.Fetch, PageSize, MaxPages, Context));

        Assert.Equal(2, fake.Calls.Count);
    }

    [Fact]
    public async Task A_response_without_total_results_still_stops_on_a_short_page()
    {
        var fake = new PagingFake(total: 1657) { OmitTotalResults = true };

        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            fake.Fetch, PageSize, MaxPages, Context);

        Assert.Equal(1657, rows.Count);
        Assert.Equal([(1000, 0), (1000, 1000)], fake.Calls);
    }

    [Fact]
    public async Task A_full_page_with_has_more_false_stops_without_probing_past_the_end()
    {
        // Regression for the exact-multiple case: no totalResults, but hasMore
        // is false. A full batch must not be read as "another page exists",
        // because NetSuite answers a past-end offset with a 404.
        static Task<NetSuiteResponse<int>> Fetch(int limit, int offset)
            => Task.FromResult(new NetSuiteResponse<int>
            {
                items = Enumerable.Range(offset + 1, limit).ToList(),
                count = limit,
                totalResults = 0,
                hasMore = false
            });

        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            Fetch, PageSize, MaxPages, Context);

        Assert.Equal(PageSize, rows.Count);
    }

    [Fact]
    public async Task A_runaway_full_page_loop_throws()
    {
        var fake = new PagingFake(total: 0)
        {
            OmitTotalResults = true,
            AlwaysReturnFullPage = true
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(fake.Fetch, PageSize, MaxPages, Context));

        Assert.Contains("exceeded 100 pages", exception.Message);
        Assert.Equal(MaxPages, fake.Calls.Count);
    }

    [Fact]
    public async Task Exact_multiple_stops_without_probing_past_the_end()
    {
        // NetSuite answers offset == totalResults with 404, never an empty page.
        var fake = new PagingFake(total: 2000);

        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            fake.Fetch, PageSize, MaxPages, Context);

        Assert.Equal(2000, rows.Count);
        Assert.Equal(2, fake.Calls.Count);
    }
}
