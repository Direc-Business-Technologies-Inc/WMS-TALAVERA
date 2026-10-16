namespace Integration.NS.Tests.Helpers;

/// <summary>
/// The ONE agreed truth table for the purchase-category visibility rule.
///
/// The rule is deliberately evaluated in two different places, because the two
/// consumers have different performance constraints:
///   - Receiving &gt; Purchase Orders pushes it down to SuiteQL.
///   - STR Transfer Order / Intercompany grids evaluate it in memory (the same
///     OR-group is a ~45s full scan on their base set).
///
/// Those two evaluators drifted apart before: the PO listing whitelisted
/// Non-Trade subcategories [4, 5, 9] while the STR grids still carried the
/// legacy [5], silently hiding non-trade office-supplies and uniforms STRs.
///
/// These cases are consumed by BOTH
/// <see cref="Helpers.PurchaseCategoryRulesTests"/> (direct rule assertions),
/// <see cref="Implementations.Transactions.StockTransferRequestIntegrationTests"/>
/// (in-memory path, end to end through the integration), and
/// <see cref="Implementations.Transactions.PurchaseOrderListingCategoryFilterTests"/>
/// (pushdown path, asserting the generated IN-list matches the whitelist).
///
/// If you change the rule, change it here first — every path fails loudly.
/// </summary>
public static class PurchaseCategoryRuleTestCases
{
    public readonly record struct Case(string Label, int? CategoryId, int? SubcategoryId, bool ExpectedToPass);

    /// <summary>
    /// Covering every truth-table cell, including the SQL three-valued-logic
    /// NULL cells that the in-memory evaluator must reproduce exactly:
    /// <c>NULL != 3</c> is UNKNOWN (false), and <c>NULL IN (4,5,9)</c> is
    /// UNKNOWN (false). A NULL category therefore only survives when the
    /// subcategory arm is unambiguously TRUE.
    /// </summary>
    public static readonly Case[] All =
    [
        // --- Trade (category 1): passes untouched, whatever the subcategory ---
        new("Trade with trade subcategory",              1,    1,    true),
        new("Trade with a non-trade subcategory",        1,    7,    true),
        new("Trade with no subcategory",                 1,    null, true),

        // --- Non-Trade (category 3): narrowed to Office/Store Supplies, Uniforms ---
        new("Non-Trade with Office Supplies (4)",        3,    4,    true),
        new("Non-Trade with Store Supplies (5)",         3,    5,    true),
        new("Non-Trade with Uniforms (9)",               3,    9,    true),
        new("Non-Trade with a non-whitelisted sub (7)",  3,    7,    false),
        new("Non-Trade with no subcategory",             3,    null, false),

        // --- NULL category: first arm UNKNOWN, second arm decides ---
        new("No category with Office Supplies (4)",      null, 4,    true),
        new("No category with Store Supplies (5)",       null, 5,    true),
        new("No category with Uniforms (9)",             null, 9,    true),
        new("No category with a non-whitelisted sub (7)",null, 7,    false),
        new("Neither category nor subcategory",          null, null, false),

        // --- Any category that is not 3 is Trade-equivalent ---
        new("Unrecognised non-3 category (2)",           2,    2,    true),
        new("Zero category",                             0,    null, true),
    ];

    /// <summary>Reference numbers for the in-memory path, in <see cref="All"/> order.</summary>
    public static string[] ReferenceNumbers { get; } =
        [.. All.Select((_, i) => $"ROW-{i:00}")];

    public static IEnumerable<object[]> AsTheoryData() =>
        All.Select(c => new object[] { c.Label, c.CategoryId, c.SubcategoryId, c.ExpectedToPass });
}