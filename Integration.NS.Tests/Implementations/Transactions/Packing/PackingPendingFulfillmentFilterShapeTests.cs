using Application.DataTransferObjects.Others.NS;
using Database.Libraries.Repositories;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Services;
using Integration.NS.Tests.Asserts;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Data;
using System.Text.RegularExpressions;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.Packing;

/// <summary>
/// Characterization tests for the "still has quantity to pack" gate on the three
/// pending-fulfillment lists.
/// <para>
/// Field defect (2026-10): a transfer order whose every line was already packed still
/// appeared on the packing list with all of its lines, and operators packed it a second
/// time. The cause is that the lists filtered on order state only —
/// <c>t.ordpicked = 'F'</c> and <c>t.status IN ('B','D','E')</c>. <c>ordpicked</c> tracks
/// <i>picking</i>, not <i>fulfilling</i>, and the transfer-order status only leaves that set
/// when the Trip Ticket ships the fulfillment; the WMS posts packing fulfillments with
/// shipStatus <c>B</c> (Packed, not Shipped). Both therefore still matched after packing.
/// </para>
/// <para>
/// The gate is <c>SuiteQLFragments.PendingPackingLines</c> — a derived table of
/// transactions with at least one open line whose ordered quantity exceeds its packed
/// quantity — inner-joined on the transaction id. It is a <b>join</b> rather than a
/// <c>WHERE</c> predicate because a correlated <c>EXISTS (... = t.id)</c> is rejected by
/// NetSuite (Rule 1, .opencode/docs/SuiteQL-Shape-Rules.md). These tests fail if that shape
/// is replaced by a correlated subquery, if the packed-quantity comparison regresses to a
/// signed one, or if the mobile scripts drift out of parity with the web filters.
/// </para>
/// </summary>
public class PackingPendingFulfillmentFilterShapeTests
{
    private static StockTransferRequestPackingIntegration CreateStr(out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new StockTransferRequestPackingIntegration(
            client,
            new HttpContextAccessor(),
            new StubSqlQueryManager(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<StockTransferRequestPackingIntegration>.Instance);
    }

    private static ReturnPackingIntegration CreateReturns(out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new ReturnPackingIntegration(
            client,
            new HttpContextAccessor(),
            new StubSqlQueryManager(),
            new SuiteQLQueryBuilderFactoryService());
    }

    private static VendorReturnAuthorizationPackingIntegration CreateVra(out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new VendorReturnAuthorizationPackingIntegration(
            client,
            new StubSqlQueryManager(),
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService());
    }

    private static DataGridIntent Intent() => new() { Skip = 0, Take = 20 };

    /// <summary>
    /// Whitespace-normalized: the derived table is a raw interpolated string keeping its own
    /// indentation, and assertions must not depend on that spacing.
    /// </summary>
    private static string Normalize(string? query)
    {
        var normalized = Regex.Replace(query!, @"\s+", " ").Trim();
        Assert.False(string.IsNullOrWhiteSpace(normalized));
        return normalized;
    }

    private static async Task<string> StrListQueryAsync()
    {
        var sut = CreateStr(out var client);
        await sut.GetPackingStockTransferRequestList(Intent(), subsidiaryId: 1);
        return Normalize(client.LastQuery);
    }

    private static async Task<string> ReturnsListQueryAsync()
    {
        var sut = CreateReturns(out var client);
        await sut.GetPackingReturnsList(Intent(), subsidiaryId: 1);
        return Normalize(client.LastQuery);
    }

    private static async Task<string> SraListQueryAsync()
    {
        var sut = CreateVra(out var client);
        await sut.GetPackingVendorReturnAuthorizationsList(Intent(), subsidiaryId: 1);
        return Normalize(client.LastQuery);
    }

    // ---------------------------------------------------------------------
    // The gate itself: an inner join onto a derived table of transactions
    // that still have an open, unpacked line, correlated in the join
    // predicate rather than inside the subquery.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Str_list_drops_fully_packed_orders_by_joining_the_open_line_derived_table()
    {
        var query = await StrListQueryAsync();

        Assert.Contains("INNER JOIN (SELECT opk.transaction AS packtran", query);
        Assert.Contains("GROUP BY opk.transaction) openpk ON openpk.packtran = t.id", query);
    }

    [Fact]
    public async Task Returns_list_drops_fully_packed_orders_by_joining_the_open_line_derived_table()
    {
        var query = await ReturnsListQueryAsync();

        Assert.Contains("INNER JOIN (SELECT opk.transaction AS packtran", query);
        Assert.Contains("GROUP BY opk.transaction) openpk ON openpk.packtran = t.id", query);
    }

    [Fact]
    public async Task Sra_list_drops_fully_packed_orders_by_joining_the_open_line_derived_table()
    {
        var query = await SraListQueryAsync();

        Assert.Contains("INNER JOIN (SELECT opk.transaction AS packtran", query);
        Assert.Contains("GROUP BY opk.transaction) openpk ON openpk.packtran = t.id", query);
    }

    // ---------------------------------------------------------------------
    // The order-state predicates are kept: the quantity gate is additive,
    // not a replacement. Without them, unpicked and wrong-category orders
    // would surface on the list.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Str_list_keeps_the_pick_and_status_predicates_alongside_the_quantity_gate()
    {
        var query = await StrListQueryAsync();

        Assert.Contains("t.ordpicked = 'F'", query);
        Assert.Contains("t.status IN ('B', 'D', 'E')", query);
        Assert.Contains("t.custbody_dbti_transfer_category IN ('1', '2')", query);
    }

    [Fact]
    public async Task Returns_list_keeps_the_pick_and_status_predicates_alongside_the_quantity_gate()
    {
        var query = await ReturnsListQueryAsync();

        Assert.Contains("t.ordpicked = 'F'", query);
        Assert.Contains("t.status IN ('B', 'D', 'E')", query);
        Assert.Contains("t.custbody_dbti_transfer_category IN ('3', '4')", query);
    }

    // ---------------------------------------------------------------------
    // The remaining-quantity test must be ABS on both sides. Transfer-order
    // lines carry negative quantities in the return direction, so a signed
    // comparison would read every return-direction line as fully packed.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Str")]
    [InlineData("Returns")]
    [InlineData("Sra")]
    public async Task Remaining_quantity_is_compared_on_absolute_values(string flow)
    {
        var query = flow switch
        {
            "Str" => await StrListQueryAsync(),
            "Returns" => await ReturnsListQueryAsync(),
            _ => await SraListQueryAsync(),
        };

        Assert.Contains("ABS(opk.quantity) - ABS(opk.", query);
        // A bare "opk.quantity -" would be the signed comparison this rule forbids.
        Assert.DoesNotContain("ABS(opk.quantity) - opk.", query);
    }

    // ---------------------------------------------------------------------
    // Closed lines are excluded: a closed line has nothing to pack, and one
    // that is closed but not fully packed (cancelled) must not keep the order
    // on the list.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Str")]
    [InlineData("Returns")]
    [InlineData("Sra")]
    public async Task The_derived_table_reads_open_item_lines_only(string flow)
    {
        var query = flow switch
        {
            "Str" => await StrListQueryAsync(),
            "Returns" => await ReturnsListQueryAsync(),
            _ => await SraListQueryAsync(),
        };

        Assert.Contains("opk.mainline = 'F'", query);
        Assert.Contains("opk.isclosed = 'F'", query);
    }

    // ---------------------------------------------------------------------
    // VRA lines are read through quantityshiprecv by their own packing line
    // script (NS_VendorReturnAuthorization_Get_Items.sql), not quantitypacked.
    // Comparing against the wrong column would hide every VRA order.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Sra_list_compares_against_the_column_its_line_script_uses()
    {
        var query = await SraListQueryAsync();

        Assert.Contains("ABS(opk.quantity) - ABS(opk.quantityshiprecv) > 0", query);
    }

    [Theory]
    [InlineData("Str")]
    [InlineData("Returns")]
    public async Task Transfer_order_lists_compare_against_quantitypacked(string flow)
    {
        var query = flow == "Str" ? await StrListQueryAsync() : await ReturnsListQueryAsync();

        Assert.Contains("ABS(opk.quantity) - ABS(opk.quantitypacked) > 0", query);
    }

    // ---------------------------------------------------------------------
    // The gate must not reintroduce correlation from the subquery into the
    // outer query's aliases: NetSuite rejects that with 400 INVALID_PARAMETER
    // "Invalid or unsupported search" (Rule 1). The only reference to the
    // outer transaction alias may sit in the join predicate.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Str")]
    [InlineData("Returns")]
    [InlineData("Sra")]
    public async Task The_derived_table_does_not_reference_the_outer_query(string flow)
    {
        var query = flow switch
        {
            "Str" => await StrListQueryAsync(),
            "Returns" => await ReturnsListQueryAsync(),
            _ => await SraListQueryAsync(),
        };

        var start = query.IndexOf("INNER JOIN (SELECT opk.transaction", StringComparison.Ordinal);
        var end = query.IndexOf("openpk ON openpk.packtran = t.id", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"expected the open-line derived-table join. Query: {query}");

        var derivedTable = query[start..end];

        Assert.DoesNotContain("t.", derivedTable);
        Assert.DoesNotContain("tl.", derivedTable);
    }

    // ---------------------------------------------------------------------
    // Structural well-formedness: the gate goes through the same builder
    // chokepoint as every other predicate, so it must not introduce a
    // trailing comma, an unbalanced paren, or an unbalanced literal.
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Str")]
    [InlineData("Returns")]
    [InlineData("Sra")]
    public async Task List_queries_remain_structurally_well_formed(string flow)
    {
        var query = flow switch
        {
            "Str" => await StrListQueryAsync(),
            "Returns" => await ReturnsListQueryAsync(),
            _ => await SraListQueryAsync(),
        };

        SuiteQLAssert.WellFormed(query, limit: 20, offset: 0);
    }

    // ---------------------------------------------------------------------
    // Parity with the mobile scripts. The MAUI app and some web paths read
    // the named scripts directly, so a web-only fix leaves the same
    // confusion on the device that operators use on the floor.
    // ---------------------------------------------------------------------
    public static IEnumerable<object[]> PendingFulfillmentScripts() =>
        new[]
        {
            new object[] { "NS_TO_x_Packing_Get_PendingFulfillment.sql", "quantitypacked" },
            new object[] { "NS_TO_x_Return_x_Packing_Get_PendingFulfillment.sql", "quantitypacked" },
            new object[] { "NS_VendorReturnAuthorization_Get_PendingReturn.sql", "quantityshiprecv" },
        }
        .Select(x => new object[] { (string)x[0], (string)x[1], ScriptsPath((string)x[0]) });

    [Theory]
    [MemberData(nameof(PendingFulfillmentScripts))]
    public void Mobile_script_carries_the_same_open_line_gate(string name, string packedColumn, string path)
    {
        var sql = Normalize(File.ReadAllText(path));

        Assert.Contains("GROUP BY opk.transaction) openpk ON openpk.packtran = t.id", sql);
        Assert.Contains($"ABS(opk.quantity) - ABS(opk.{packedColumn}) > 0", sql);
        Assert.Contains("opk.isclosed = 'F'", sql);
    }

    // The scripts keep their own order-state predicates; the gate is additive.
    [Theory]
    [MemberData(nameof(PendingFulfillmentScripts))]
    public void Mobile_script_keeps_its_order_state_predicates(string name, string packedColumn, string path)
    {
        var sql = Normalize(File.ReadAllText(path));

        Assert.Contains("t.recordtype", sql);
        Assert.Contains("t.status IN", sql);
    }

    private static string ScriptsPath(string fileName)
    {
        var copied = Path.Combine(AppContext.BaseDirectory, "Scripts", fileName);
        if (File.Exists(copied)) return copied;

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Integration.NS", "NSScripts", fileName);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException($"Could not locate NSScripts/{fileName}.");
    }

    private sealed class StubSqlQueryManager : ISqlQueryManager
    {
        public Dictionary<string, string> GetSqlScriptWithMetadata(string scriptFileName, out string sqlContent, out bool isScriptFound, string scriptFilePath)
            => throw new NotSupportedException();

        public Dictionary<string, string> SetSqlScriptWithMetadata(string scriptFileName, string scriptContent, Dictionary<string, string> metadata, string scriptFilePath)
            => throw new NotSupportedException();

        public Task<int> ExecuteAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<int> ExecuteAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> QueryAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> QueryAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> RawQueryAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> RawQueryOneAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> RawQueryAsync<T, U>(string query, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> QueryOneAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> QueryOneAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<Dictionary<string, object>>> QueryAsync(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
    }
}