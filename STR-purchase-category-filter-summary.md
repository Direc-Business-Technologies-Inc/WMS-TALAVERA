# Purchase Category Visibility Rule — Ported to STR

**Date:** 2026-10-15
**Feature:** Non-Trade purchase-category filtering applied consistently across the Purchase Order and Stock Transfer Request listings
**Status:** Implemented, 316/316 tests pass

---

## 1. The problem

Two listings were supposed to show the same set of transactions. They didn't.

The rule, agreed with the business on 2026-10-13:

> Trade transactions pass untouched. Non-Trade transactions pass **only** when their subcategory is
> Office Supplies (4), Store Supplies (5), or Uniforms (9).

- **Receiving → Purchase Orders** got the full rule on 2026-10-13 — whitelist `[4, 5, 9]`.
- **STR → Transfer Orders / Intercompany** kept the legacy predicate from before that meeting — whitelist `[5]` only.

The result was silent. A non-trade STR for office supplies or uniforms existed in NetSuite, was
perfectly valid, and simply **never appeared on the grid**. No error, no warning, no audit trail.
Users filed it as "some of our requests are missing."

The two implementations had drifted because the rule was written down twice:

| | Category constant | Subcategory whitelist |
|---|---|---|
| `ReceivingIntegration.cs` | `3` | `["4", "5", "9"]` |
| `StockTransferRequestIntegration.cs` | `3` | `5` |

Two literals, two files, no shared definition, no test tying them together.

---

## 2. The fix

### 2.1 One definition of the rule

New: `Integration.NS/Helpers/PurchaseCategoryRules.cs`

```csharp
internal static class PurchaseCategoryRules
{
    public const int TradeCategoryId = 1;
    public const int NonTradeCategoryId = 3;

    public static readonly int[] NonTradeWhitelistedSubcategoryIds = [4, 5, 9];

    public static bool Passes(int? categoryId, int? subcategoryId) =>
        (categoryId.HasValue && categoryId.Value != NonTradeCategoryId)
        || (subcategoryId.HasValue && NonTradeWhitelistedSubcategoryIds.Contains(subcategoryId.Value));
}
```

### 2.2 Both consumers now read from it

`StockTransferRequestIntegration.cs` — the fix itself:

```csharp
private static bool PassesPurchaseCategoryFilter(StockTransferRequestDataGridNSDTO row) =>
    PurchaseCategoryRules.Passes(row.PurchaseCategoryId, row.PurchaseSubcategoryId);
```

`ReceivingIntegration.cs` — unchanged generated SQL, now sourced from the shared rule:

```csharp
private const int NonTradePurchaseCategoryId = PurchaseCategoryRules.NonTradeCategoryId;
private static readonly string[] NonTradeSubcategoryIds =
    [.. PurchaseCategoryRules.NonTradeWhitelistedSubcategoryIds.Select(id => id.ToString())];
```

### 2.3 Why the two consumers still evaluate differently

**This is deliberate. Do not "simplify" it by pushing the predicate into the STR query.**

The rule is *the same*. The *evaluation strategy* is not, and must not be:

| | PO listing | STR TO / ICTO grids |
|---|---|---|
| Strategy | SuiteQL pushdown | In-memory, after full fetch |
| Why | Base set is selective; pushdown is fast | The OR-group is a **~45s full scan** on this base set |
| Measured | See open risk below | Q0=46s, Q1=26s, Q5-without-it=1–2s, Q6-date-bound=47–48s |

A date bound does not rescue it (Q6 stayed at 47–48s), so "add a filter and push it down" is not
an option here.

The STR path therefore still fetches the whole selective base set
(`FetchAllBaseRowsAsync`, 1000-row pages, 5000-row cap), applies the rule in memory, then sorts and
pages the *filtered* set (`SliceFilteredPage`) so counts and page boundaries stay correct. That
scaffolding is unchanged by this fix.

**Widening the whitelist costs zero extra NetSuite work.** It can only remove rows from the
filtered set; the base set and therefore the fetch cost are untouched.

---

## 3. SQL three-valued logic

`PurchaseCategoryRules.Passes` reproduces NetSuite's `WHERE` semantics exactly, because the
pushdown consumer gets 3VL from the database and the two must agree:

| Expression | SQL | C# (naive) | Correct C# |
|---|---|---|---|
| `NULL != 3` | UNKNOWN → false | **`true`** (lifted `!=`) | `false` |
| `NULL IN (4,5,9)` | UNKNOWN → false | `false` | `false` |

The `HasValue` guard on the first arm is **load-bearing**. C#'s lifted `!=` on `int?` returns `true`
for `null`, which would have let every un-categorised STR through. This was already correct in the
old STR code and is preserved verbatim — it is now documented and shared rather than incidental.

Net effect: a transaction with **no** purchase category is hidden **unless** its subcategory is
unambiguously one of 4/5/9.

---

## 4. Regression coverage

New: `Integration.NS.Tests/Helpers/PurchaseCategoryRuleTestCases.cs` — a single truth table covering
every cell, including all the NULL cases, consumed by all three test suites:

| Suite | What it proves |
|---|---|
| `PurchaseCategoryRulesTests` | The rule itself, case by case |
| `StockTransferRequestIntegrationTests` | The in-memory path is wired to the shared rule and the right DTO properties |
| `PurchaseOrderListingCategoryFilterTests` | The pushdown IN-list is generated *from* the whitelist, not hardcoded |

The PO suite now asserts `IN ('4', '5', '9')` is derived from
`PurchaseCategoryRules.NonTradeWhitelistedSubcategoryIds` — so if the whitelist changes and the PO
path is not updated, **the build fails** rather than the two paths diverging silently again.

### Mutation-verified

Reverting the whitelist to the legacy `[5]` fails **9 tests** across all three suites. The coverage
has teeth; it is not decorative.

---

## 5. Files changed

- `Integration.NS/Helpers/PurchaseCategoryRules.cs` — **new**, the single source of truth
- `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs` — delegates to the shared rule
- `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` — constants sourced from the shared rule (generated SQL byte-identical)
- `Integration.NS.Tests/Helpers/PurchaseCategoryRuleTestCases.cs` — **new**, shared truth table
- `Integration.NS.Tests/Helpers/PurchaseCategoryRulesTests.cs` — **new**, direct rule assertions
- `Integration.NS.Tests/Implementations/Transactions/StockTransferRequestIntegrationTests.cs` — widened cases + end-to-end truth-table run
- `Integration.NS.Tests/Implementations/Transactions/PurchaseOrderListingCategoryFilterTests.cs` — pushdown parity assertions

No DTO, handler, VM, or Razor changes. Both STR base queries already selected the raw
`PurchaseCategoryId` / `PurchaseSubcategoryId` needed by the in-memory predicate.

---

## 6. Validation

- `dotnet build WMS.slnx` — **0 errors** (2688 warnings, all pre-existing)
- `Integration.NS.Tests` — **316/316 pass**
- Mutation check — reverting the whitelist fails 9 tests; restoring returns to green
- Data integrity — **read-only change.** No mutation, payload, or approval path touched.

### Manual verification required

There is no test project for `Web.BlazorServer`, so the rendered grid needs a human:

1. Open **STR → Transfer Orders** as a user scoped to a subsidiary with non-trade STRs.
2. Confirm non-trade **Office Supplies (4)** and **Uniforms (9)** rows now appear.
3. Confirm a non-trade row with any other subcategory still does **not** appear.
4. Confirm the **Purchase Category** column shows a sensible value for the newly-visible rows — this
   is what tells the user *why* the row is visible, and it was the point of the original PO fix.
5. Repeat on the **Intercompany** tab.
6. Spot-check row counts and page boundaries (the grid pages the filtered set, not the base set).

---

## 7. Open risks — **not fixed here**

1. **The PO listing may have a ~45s query and nobody has checked.**
   The test doc comment in `PurchaseOrderListingCategoryFilterTests.cs` flags this as a live risk:
   run the PO base set through the SuiteQL workbench with and without the OR-group. The STR base set
   measured 26–46s with the *identical* OR-group shape; the PO base set is larger. **This is more
   urgent than the gap that was just closed** and is still unmeasured.

2. **The STR Returns tab is unfiltered.**
   `GetReturnsList` (`StockTransferRequestIntegration.cs`) has **no** purchase-category predicate at
   all, and does not even select the raw category ids. If the rule is meant to apply to Returns, it
   needs the full `FetchAllBaseRowsAsync` → filter → `SliceFilteredPage` treatment, because that
   query still uses server-side `ExecuteWithPaging`. **Deliberately left out of this change —
   scope was confirmed as TO + ICTO only.**

3. **Grid column filters on the STR grids can emit invalid SQL.**
   `GetTransferOrderList` / `GetIntercompanyTransferOrderList` call `.WithDatagridIntent(intent)`
   with **no column map**, unlike `ReceivingIntegration`, which passes `PurchaseOrderColumnMap`.
   A grid filter on `PurchaseCategory`, `PurchaseSubcategory`, or `Subsidiary` puts a SELECT alias
   into `WHERE`, which NetSuite will not resolve. Pre-existing and unrelated — but do not copy
   this omission into new code.

4. **`PurchaseCategoryRules.NonTradeWhitelistedSubcategoryIds` is a mutable `static readonly` array.**
   Consistent with the existing `NonTradeSubcategoryIds` pattern. Safe as long as nobody mutates it
   in place. A frozen `ImmutableArray<int>` would close the hole if this ever gets more consumers.

---

## 8. NetSuite id fragility

`3`, `4`, `5`, `9` are **ordinal inserts** into `CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST` and
`CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES`. Reordering either list in NetSuite changes what these ids
mean. There is no migration hook and no error — the rule would simply start admitting the wrong
transactions.

If these lists are ever reordered, the constants in `PurchaseCategoryRules` must be updated in the
same change, and this is exactly the scenario the shared-definition refactor is designed to make
visible: one file to check instead of two.