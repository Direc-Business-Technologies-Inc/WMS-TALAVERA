using Integration.NS.Helpers;
using Integration.NS.Tests.Helpers;
using Xunit;

namespace Integration.NS.Tests.Helpers;

/// <summary>
/// Direct assertions on <see cref="PurchaseCategoryRules.Passes"/> — the rule
/// that both the Receiving &gt; Purchase Orders listing (SuiteQL pushdown) and
/// the STR Transfer Order / Intercompany grids (in-memory) now depend on.
///
/// This is the regression guard for the 2026-10-15 fix, where the two paths had
/// silently diverged: PO whitelisted Non-Trade subcategories [4, 5, 9] while STR
/// still carried the legacy [5], hiding non-trade office-supplies and uniforms.
/// </summary>
public class PurchaseCategoryRulesTests
{
    [Theory]
    [MemberData(nameof(PurchaseCategoryRuleTestCases.AsTheoryData), MemberType = typeof(PurchaseCategoryRuleTestCases))]
    public void Passes_matches_the_agreed_truth_table(
        string label, int? categoryId, int? subcategoryId, bool expected)
    {
        Assert.Equal(expected, PurchaseCategoryRules.Passes(categoryId, subcategoryId));
    }

    [Fact]
    public void Whitelist_is_office_supplies_store_supplies_and_uniforms()
    {
        Assert.Equal([4, 5, 9], PurchaseCategoryRules.NonTradeWhitelistedSubcategoryIds);
    }

    [Fact]
    public void NonTrade_category_id_is_three()
    {
        Assert.Equal(3, PurchaseCategoryRules.NonTradeCategoryId);
    }
}