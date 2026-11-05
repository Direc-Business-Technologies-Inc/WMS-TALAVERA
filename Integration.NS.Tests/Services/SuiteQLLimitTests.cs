using Integration.NS.Helpers;
using Integration.NS.Implementations.Others;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Regression coverage for NetSuite SuiteQL limit/offset bounds.
///
/// Bug: the create-user page requested the employee list with
/// <c>DataGridIntent { Take = int.MaxValue }</c>, which reached the SuiteQL REST
/// endpoint as <c>?limit=2147483647</c> and was rejected with
/// 400 INVALID_PARAMETER ("The specified query 'limit' is out of bounds. Provide
/// value between 1 and 1000"), leaving the employee dropdown empty and blocking
/// user creation.
///
/// Invariant under test: every limit that leaves the integration layer is either
/// <c>null</c> (parameter omitted → NetSuite default page) or within 1..1000, and
/// every non-null limit is paired with an offset NetSuite accepts (offset must be
/// divisible by limit — see the doc comment on SuiteQLQueryExtensions).
/// </summary>
public class SuiteQLLimitTests
{
    private const int NetSuiteMinLimit = 1;
    private const int NetSuiteMaxLimit = 1000;

    private static SuiteQLQueryBuilderFactoryService Factory => new();

    [Theory]
    [InlineData(-2, null)]
    [InlineData(-1, null)]           // codebase "unlimited" convention (TripTicket, login locations)
    [InlineData(0, null)]            // DataGridIntentAdapter can produce Take = 0
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(1500, 1000)]
    [InlineData(int.MaxValue, 1000)] // the reported bug
    public void Build_NormalizesTakeToNetSuiteLimitBounds(int take, int? expectedLimit)
    {
        var query = Factory.Create()
            .From("employee")
            .WithDatagridIntent(new DataGridIntent { Take = take })
            .Build();

        Assert.Equal(expectedLimit, query.Limit);
        Assert.True(
            query.Limit is null or >= NetSuiteMinLimit and <= NetSuiteMaxLimit,
            $"Build produced Limit={query.Limit}; NetSuite accepts null or {NetSuiteMinLimit}..{NetSuiteMaxLimit}");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5, 0)]
    [InlineData(1000, 0)]
    [InlineData(1000, 1000)]
    [InlineData(1000, 1500)]        // unaligned offset → stitching path
    [InlineData(1500, 1500)]        // clamped to 1000, offset realigned by stitching
    [InlineData(int.MaxValue, 0)]   // the reported bug, end to end through ExecuteWithPaging
    public async Task ExecuteWithPaging_SendsOnlyValidAlignedLimitOffset(int take, int skip)
    {
        var client = new CapturingNetSuiteClient();
        var query = Factory.Create()
            .From("employee")
            .WithDatagridIntent(new DataGridIntent { Take = take, Skip = skip })
            .Build();

        await query.ExecuteWithPaging<object>(client);

        Assert.NotEmpty(client.Calls);
        foreach (var (limit, offset) in client.Calls)
        {
            if (limit is null)
            {
                // Parameter omitted — NetSuite applies its default page. Alignment
                // is only assertable when we control the limit ourselves.
                continue;
            }

            Assert.InRange(limit.Value, NetSuiteMinLimit, NetSuiteMaxLimit);
            Assert.True(
                offset is null || offset.Value % limit.Value == 0,
                $"offset {offset} is not divisible by limit {limit}; NetSuite rejects that combination");
        }
    }

    [Fact]
    public async Task ExecuteWithPaging_UnalignedOffset_StitchesTwoAlignedPages()
    {
        var client = new CapturingNetSuiteClient();
        var query = Factory.Create()
            .From("employee")
            .WithDatagridIntent(new DataGridIntent { Take = 1000, Skip = 1500 })
            .Build();

        await query.ExecuteWithPaging<object>(client);

        Assert.Equal(2, client.Calls.Count);
        Assert.Equal(1000, client.Calls[0].Limit);
        Assert.Equal(1000, client.Calls[0].Offset);
        Assert.Equal(1000, client.Calls[1].Limit);
        Assert.Equal(2000, client.Calls[1].Offset);
    }

    [Fact]
    public async Task EmployeeList_WithIntMaxValueTake_NeverRequestsOutOfBoundsLimit()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new EmployeeIntegration(client, Factory);

        await sut.GetEmployeesListAsync(new DataGridIntent { Take = int.MaxValue });

        Assert.NotEmpty(client.Calls);
        foreach (var (limit, _) in client.Calls)
        {
            Assert.NotNull(limit);
            Assert.InRange(limit!.Value, NetSuiteMinLimit, NetSuiteMaxLimit);
        }
    }
}