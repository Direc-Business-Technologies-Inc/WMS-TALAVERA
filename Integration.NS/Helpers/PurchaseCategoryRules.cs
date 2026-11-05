namespace Integration.NS.Helpers;

/// <summary>
/// The single source of truth for which transactions are visible in the
/// trade/non-trade purchase-category filtered listings.
///
/// Backed by NetSuite CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST and
/// CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES ids. These are ORDINAL INSERTS —
/// reordering the custom lists in NetSuite breaks these values silently, with
/// no error and no migration hook. Do not "tidy" these numbers.
///
/// Rule (colleague-confirmed 2026-10-13, applied to the Receiving &gt; Purchase
/// Orders listing first, ported to the STR Transfer Order / Intercompany grids
/// 2026-10-15):
///   - Trade transactions (category != 3) pass untouched.
///   - Non-Trade transactions (category 3) pass ONLY when their subcategory is
///     one of Office Supplies (4), Store Supplies (5), Uniforms (9).
///
/// The rule is evaluated in two different places, for two different reasons.
/// That split is deliberate — see the notes on each consumer:
///   - ReceivingIntegration pushes it down to SuiteQL as an OR-group.
///   - StockTransferRequestIntegration evaluates it in memory, because the same
///     OR-group over the TO/ICTO base set resolves as a ~45s full scan in
///     NetSuite (Q0=46s, Q1=26s, Q5-without-it=1-2s, Q6-date-bound=47-48s —
///     see STR-grid-perf-fix-summary.md).
///
/// Because the two evaluators are inherently separate, they share THIS file and
/// the truth table in PurchaseCategoryRulesTests. That shared test is the only
/// thing guaranteeing the two paths cannot drift apart again — they previously
/// did, and the divergence is what this refactor exists to fix.
/// </summary>
internal static class PurchaseCategoryRules
{
    /// <summary>
    /// Trade (1) and Non-Trade (3) are the two categories that carry a
    /// meaningful subcategory. Any other category id passes the filter by virtue
    /// of not being <see cref="NonTradeCategoryId"/>.
    /// </summary>
    public const int TradeCategoryId = 1;

    /// <summary>Non-Trade. The only category subject to subcategory narrowing.</summary>
    public const int NonTradeCategoryId = 3;

    /// <summary>
    /// Non-Trade subcategories that remain visible in the narrowed listings:
    /// Office Supplies (4), Store Supplies (5), Uniforms (9).
    ///
    /// NOTE: this list was historically just <c>[5]</c> (Store Supplies) in the
    /// STR grids while the PO listing had already widened to all three. The
    /// mismatch silently hid non-trade office-supplies and uniforms STRs.
    /// </summary>
    public static readonly int[] NonTradeWhitelistedSubcategoryIds = [4, 5, 9];

    /// <summary>
    /// The single in-memory equivalent of the SuiteQL predicate
    /// <c>(purchase_category != 3 OR purchase_subcategory IN (4, 5, 9))</c>.
    ///
    /// NULL semantics mirror SQL three-valued logic exactly, because that is
    /// what the pushdown consumer gets from NetSuite and the two MUST agree:
    ///   - <c>NULL != 3</c> is UNKNOWN, i.e. FALSE in a WHERE clause. C#'s lifted
    ///     <c>!=</c> on <c>int?</c> would wrongly return TRUE for null, so the
    ///     <see cref="Nullable{T}.HasValue"/> guard is load-bearing — do not
    ///     "simplify" it away.
    ///   - <c>NULL IN (4, 5, 9)</c> is UNKNOWN, i.e. FALSE. Guarded explicitly
    ///     here for the same reason even though C#'s <c>Contains</c> on a null
    ///     already yields false.
    /// </summary>
    /// <param name="categoryId">Raw <c>custbody_dbti_purchase_category</c> id.</param>
    /// <param name="subcategoryId">Raw <c>custbody_dbti_purchase_subcategory</c> id.</param>
    public static bool Passes(int? categoryId, int? subcategoryId) =>
        (categoryId.HasValue && categoryId.Value != NonTradeCategoryId)
        || (subcategoryId.HasValue && NonTradeWhitelistedSubcategoryIds.Contains(subcategoryId.Value));
}