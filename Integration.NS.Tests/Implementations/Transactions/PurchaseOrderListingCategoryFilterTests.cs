using Integration.NS.Implementations.Transactions;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

/// <summary>
/// Feature 2026-10-13: the Receiving > Purchase Orders listing is narrowed so
/// that Trade POs (purchase category != 3) pass untouched, while Non-Trade
/// (category 3) POs only pass when their subcategory is one of
/// Office Supplies (4), Store Supplies (5), Uniforms (9).
///
/// The OR-group predicate IS pushed to NetSuite (unlike STR's TO/ICTO grids,
/// which run it in memory — see StockTransferRequestIntegrationTests). This is
/// the measured-decision point: if the SuiteQL workbench shows the OR-group
/// scanning ~45s on the PO base set, this query must move to the in-memory
/// PassesPurchaseCategoryFilter pattern instead.
/// </summary>
public class PurchaseOrderListingCategoryFilterTests
{
    private static (ReceivingIntegration Sut, CapturingNetSuiteClient Client) CreateCapturing()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);
        return (sut, client);
    }

    [Fact]
    public async Task Purchase_order_list_query_carries_the_category_or_group()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetPurchaseOrdersListAsync(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        var where = client.LastQuery
            .Split("WHERE", StringSplitOptions.RemoveEmptyEntries)
            .Last();

        // Exact generated shape, including the parentheses that keep the
        // OR-group from binding with the surrounding ANDs.
        Assert.Contains(
            "(t.custbody_dbti_purchase_category != 3 OR t.custbody_dbti_purchase_subcategory IN ('4', '5', '9'))",
            where);
    }

    [Fact]
    public async Task Purchase_order_list_query_excludes_custom_closed_status_but_allows_unset_status()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetPurchaseOrdersListAsync(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        var query = System.Text.RegularExpressions.Regex.Replace(client.LastQuery, @"\s+", " ");
        Assert.Contains(
            "(t.custbody_dbti_custom_approval_status IS NULL OR t.custbody_dbti_custom_approval_status != '20')",
            query);
        Assert.Contains("t.status IN ('B', 'E')", query);
    }

    /// <summary>
    /// 2026-10-15: the pushdown IN-list is derived from the shared
    /// <c>PurchaseCategoryRules</c> whitelist rather than hardcoded, so the PO
    /// listing and the STR grids cannot drift apart again. This asserts the two
    /// paths agree on the exact same subcategory set.
    /// </summary>
    [Fact]
    public async Task Purchase_order_pushdown_in_list_matches_the_shared_whitelist()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetPurchaseOrdersListAsync(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);

        var expected = string.Join(
            ", ",
            PurchaseCategoryRules.NonTradeWhitelistedSubcategoryIds
                .Select(id => $"'{id}'"));

        Assert.Contains($"IN ({expected})", client.LastQuery);
    }

    /// <summary>
    /// Same rule, same constant — the two consumers must agree on the
    /// Non-Trade category id as well as the subcategory whitelist.
    /// </summary>
    [Fact]
    public async Task Purchase_order_pushdown_category_id_matches_the_shared_rule()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetPurchaseOrdersListAsync(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains($"!= {PurchaseCategoryRules.NonTradeCategoryId}", client.LastQuery);
    }

    [Fact]
    public async Task Purchase_order_list_query_selects_category_display_columns()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetPurchaseOrdersListAsync(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains("AS PurchaseCategory", client.LastQuery);
        Assert.Contains("AS PurchaseSubcategory", client.LastQuery);
    }
}
