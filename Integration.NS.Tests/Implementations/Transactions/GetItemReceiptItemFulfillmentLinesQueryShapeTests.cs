using System.Text.RegularExpressions;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

/// <summary>
/// Bugfix 2026-10-13: the STR/Returns web item-receipt line load 400'd at
/// NetSuite with <c>400 [INVALID_PARAMETER] ... Search error occurred: Invalid
/// or unsupported search</c> because the <c>LineNumber</c> and
/// <c>QuantityReceived</c> select items of
/// <see cref="ReceivingIntegration.GetItemReceiptItemFulfillmentLinesAsync"/>
/// were correlated scalar subqueries — the QuantityReceived one nested two
/// levels deep, referencing the outer <c>tl</c> and <c>uom</c> aliases. The PO
/// path (<c>GetItemReceiptLinesAsync</c>) never had them, which is why the
/// error tracked "specific users": it tracked the Item Fulfillment branch that
/// only the STR/Returns flow reaches. See .opencode/docs/SuiteQL-Shape-Rules.md
/// (Rule 1).
/// </summary>
public class GetItemReceiptItemFulfillmentLinesQueryShapeTests
{
    private static (ReceivingIntegration Sut, CapturingNetSuiteClient Client) Create()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);
        return (sut, client);
    }

    /// <summary>
    /// Builds the IF-branch query and returns it whitespace-normalized: the SQL
    /// fragments keep raw-string indentation that FormatQuery only collapses at
    /// request time, and assertions must not depend on that spacing.
    /// </summary>
    private static async Task<string> BuiltQueryAsync(string docEntry)
    {
        var (sut, client) = Create();
        await sut.GetItemReceiptItemFulfillmentLinesAsync(docEntry);
        var normalized = Regex.Replace(client.LastQuery!, @"\s+", " ").Trim();
        Assert.False(string.IsNullOrWhiteSpace(normalized));
        return normalized;
    }

    // The IF → TO line mapping must ride in a derived table joined on its own
    // output columns — the same proven shape as the preferred-bin (pb) join.
    [Fact]
    public async Task Query_resolves_the_if_to_to_line_via_a_derived_table_on_its_own_outputs()
    {
        var query = await BuiltQueryAsync("IF-1");

        Assert.Contains(") ifmap ON ifmap.ifdoc = tl.transaction AND ifmap.ifline = tl.id", query);
        Assert.Contains("ifmap.tolineid AS LineNumber", query);
    }

    [Fact]
    public async Task QuantityReceived_reads_fulfillment_allocations_without_a_nested_subquery()
    {
        var query = await BuiltQueryAsync("IF-1");

        // Receipt totals are tied to the selected IF, not taken from the shared
        // Transfer Order quantityshiprecv counter.
        Assert.Contains("NVL(ifmap.qtyrecv / uom.conversionrate, 0) AS QuantityReceived", query);
        Assert.Contains("ifreceipt.previousdoc AS ifdoc", query);
        Assert.Contains("nexttransactionlink ifreceipt", query);
        Assert.Contains("ifreceipt.linktype = 'TOrdCost'", query);
        Assert.Contains("SUM(ABS(receiptline.quantity)) AS qtyrecv", query);
        Assert.DoesNotContain("ttl.quantityshiprecv", query);
        Assert.DoesNotContain("rline.id =", query);
        Assert.DoesNotContain("SELECT ttl.id", query);
    }

    // Rule 1 of the SuiteQL shape rules: an outer alias must not be referenced
    // from inside a subquery. The SELECT list (everything between SELECT and the
    // driving FROM) must therefore be correlation-free — no scalar subqueries at
    // all, since both former ones were correlated.
    [Fact]
    public async Task Select_list_contains_no_correlated_subqueries()
    {
        var query = await BuiltQueryAsync("IF-1");

        var fromIndex = query.IndexOf(" FROM transactionline tl", StringComparison.Ordinal);
        Assert.True(fromIndex > 0, $"expected the driving FROM clause. Query: {query}");
        var selectList = query[..fromIndex];

        Assert.DoesNotContain("(SELECT", selectList);
        Assert.Contains("ifmap.tolineid AS LineNumber", selectList);
        Assert.Contains("NVL(ifmap.qtyrecv / uom.conversionrate, 0) AS QuantityReceived", selectList);
    }

    // Rule 1, table side: the ifmap derived table itself must resolve the whole
    // IF → TO mapping internally — none of the outer aliases (tl, uom) may appear
    // inside it. (Its own internal joins reference their internal aliases, which
    // is the accepted shape.)
    [Fact]
    public async Task The_ifmap_derived_table_resolves_everything_internally()
    {
        var query = await BuiltQueryAsync("IF-1");

        var start = query.IndexOf("LEFT JOIN ( SELECT tolink.nextdoc", StringComparison.Ordinal);
        var end = query.IndexOf(") ifmap ON", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"expected the ifmap derived-table join. Query: {query}");
        var derivedTable = query[start..end];

        // " tl." is space-delimited because the table's internal alias (ttl.)
        // contains "tl." as a plain substring.
        Assert.DoesNotContain(" tl.", derivedTable);
        Assert.DoesNotContain("uom.", derivedTable);
        Assert.Contains("ABS(ttl.quantity) AS quantityplanned", derivedTable);
        Assert.Contains("ABS(ifline.quantity) AS quantityfulfilled", derivedTable);
        Assert.Contains("NVL(receipts.qtyrecv, 0) AS qtyrecv", derivedTable);
    }

    [Fact]
    public async Task Query_still_filters_on_the_fulfillment_tranid()
    {
        var query = await BuiltQueryAsync("IF-THM00100000660");

        Assert.Contains("WHERE t.tranid = 'IF-THM00100000660'", query);
    }

    [Fact]
    public async Task Fulfillment_line_plan_uses_its_shipped_quantity()
    {
        var query = await BuiltQueryAsync("IF-1");

        Assert.Contains("ABS(ifmap.quantityplanned / uom.conversionrate) AS QuantityPlanned", query);
        Assert.Contains("ABS(ifmap.quantityfulfilled / uom.conversionrate) AS QuantityFulfilled", query);
    }
}
