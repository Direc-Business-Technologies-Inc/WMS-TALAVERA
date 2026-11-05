# Fix: Returns packing transfer-category blocker + null-`items` crash in packing lookups

Date: 2026-10-16
Status: Both fixes in source, build clean, **340/340 tests green** — awaiting client verification
Mode: Bugfix / Follow-up to [2026-10-16 packing bin guard fix](2026-10-16-packing-bin-guard-false-positive-fix.md)
Scope: Packing module — Returns packing (category resolution) and all three packing integrations (SuiteQL result handling)
Related: [2026-10-16 packing bin guard fix](2026-10-16-packing-bin-guard-false-positive-fix.md); [2026-10-15 IF replace-param fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) (introduced the category guard this fixes)
Changed files:
- `Integration.NS/NSScripts/NS_TO_x_Return_x_Packing_Get_Items.sql`
- `Integration.NS/DataTransferObjects/Packing/Returns/ReturnPackingLineNSDTO.cs`
- `Integration.DataTransferObjects/Transactions/Packing/Returns/ReturnsLineDTO.cs`
- `Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs`
- `Integration.NS/Implementations/Transactions/Packing/StockTransferRequestPackingIntegration.cs`
- `Integration.NS/Implementations/Transactions/Packing/VendorReturnAuthorizationPackingIntegration.cs`
- `Integration.NS.Tests/Fakes/CapturingNetSuiteClient.cs`
- `Integration.NS.Tests/Implementations/Transactions/Packing/PackingNullItemsTests.cs` (new)

## Summary

Two independent defects found while verifying the bin-guard fix in the field. Both were surfaced by hand during the same session and are unrelated to each other.

| # | Symptom | Severity |
|---|---|---|
| **A** | Returns packing **cannot be saved at all** on any Good line — *"unrecognised transfer category (0)"* | **Blocks a whole flow** |
| **B** | Packing save crashes with *"Value cannot be null. (Parameter 'source')"* — a raw LINQ message on the operator's screen | Intermittent; ambiguous |

Defect A was introduced by `3ed7f153` (2026-10-15). Defect B is pre-existing and predates it.

---

## Defect A — Returns packing blocked: the category field was never projected

### Symptom

```
Fulfill Returns Item failed: Return STR-THM00100000684 line 2
has an unrecognised transfer category (0). Nothing was saved.
Please reload the order and try again.
```

### Root cause

The fail-closed category guard added on 2026-10-15 reads `line.TransferCategory`:

```csharp
// PostReturnsIFCmd.cs:61-62
var badCategory = request.Data.FirstOrDefault(line =>
    !line.IsBad && !line.IsMissing && !InventoryStatusMap.IsKnownTransferCategory(line.TransferCategory));
```

`IsKnownTransferCategory` accepts only `3` (Returns Good) or `4` (Returns Bad). But the field is **never populated**, so it is always `0` and the guard rejects every line that isn't explicitly flagged Bad or Missing.

Three gaps in the chain:

1. **The SQL filtered on the category without projecting it.** `NS_TO_x_Return_x_Packing_Get_Items.sql` had `AND t.custbody_dbti_transfer_category IN ('3','4')` in its `WHERE` (line 159) but no matching column in its `SELECT`. The query returned rows whose lines all defaulted the field.
2. **The grid mapper never assigned it.** `ReturnPackingIntegration.MapLineDto` built `ReturnsLineDTO` from eight properties, none of them the category.
3. **The grid DTO had no such property.** `Application.DataTransferObjects.Transactions.Packing.Returns.ReturnsLineDTO` carried only the grid columns.

Note there are **two** `ReturnsLineDTO` classes — this one and `Application.DataTransferObjects.Transactions.Commons.NS.ReturnsLineDTO`. Only the `Commons.NS` one had ever declared `TransferCategory`. The packing flow's active path (`GetReturnsLineQry`) resolves to the `Commons.NS` type, which is why the web/mobile item load compiled and bound correctly once the SQL projected the column.

### Impact

- Every Returns order with at least one Good line **fails to save**. The guard cannot be satisfied by any input.
- Before `3ed7f153`, the same orders saved — but `InventoryStatusMap.Resolve(false, false, 0)` fell through to `Good`, so a **Returns-Bad order was silently booked as Good stock**. That was the bug `3ed7f153` set out to fix; it converted a silent mis-post into a hard block without closing the data gap.
- `PostTransferOrderIFCmd` and `PostVendorReturnAuthorizationIFCmd` have no equivalent guard, so STR and VRA still resolve category `0` to `Good` silently.

### The fix

**1. Project the column** — `NS_TO_x_Return_x_Packing_Get_Items.sql`

```sql
t.status AS OrderStatus,
t.custbody_dbti_transfer_category AS TransferCategory,
```

Selected **raw**, not via `BUILTIN.DF`, because the guard compares against the internal id (`3`/`4`) while `BUILTIN.DF` yields the display name. This mirrors `StockTransferRequestIntegration.cs:253`, which already binds the same field to an `int` DTO in production.

**2. Grid DTO + mapper** — `ReturnPackingLineNSDTO.TransferCategory` (`int`) added, selected at `ReturnPackingIntegration.cs` as `("q.TransferCategory", …)`, and mapped in `MapLineDto`.

**3. Grid line DTO** — `Packing.Returns.ReturnsLineDTO.TransferCategory` (`int`) added, so the grid path carries the same value the item path already did.

Both web and mobile Returns packing read through `GetReturnsLineQry` (`Application.UseCases/Queries/Transaction/Packing/NS/Returns/GetReturnsLineQry.cs:24`), which calls `NetsuiteQuery<Commons.NS.ReturnsLineDTO>` directly on this script — so the SQL change alone restores both entry points.

### Deliberately out of scope

The equivalent STR fix. `NS_TO_x_Packing_Get_Items.sql` has the same gap and `PostTransferOrderIFCmd` has no guard — adding the guard before projecting the column would hard-block STR packing. Both halves are needed together; neither is safe alone.

---

## Defect B — `ArgumentNullException (Parameter 'source')` on packing save

### Symptom

Field test on `24161b88`, order `STR-THM00100000675`, mobile:

```
Value cannot be null. (Parameter 'source')
```

Lines loaded normally, the operator scanned and pressed Save, and the error appeared with no other context.

### Root cause

`MakeRequest` can return a response object whose `items` array is null:

```csharp
// NetSuiteApiClientService.cs:427-435
if (httpResponse.IsSuccessStatusCode)
{
    if (string.IsNullOrEmpty(responseJson))
        return default!;                       // T = NetSuiteResponse<T>, so null
    var response = JsonSerializer.Deserialize<T>(responseJson, ...);
    return response ?? throw new Exception("Bad response from NetSuite API");
}
```

A **2xx body that omits the `items` array** — an error envelope, a partial response — deserializes into a non-null `NetSuiteResponse<T>` whose `items` is null. Three packing lookups then called a LINQ operator on it:

```csharp
// StockTransferRequestPackingIntegration.cs:170
var row = response.items.FirstOrDefault();     // ArgumentNullException, ParamName "source"
```

`Enumerable.FirstOrDefault` throws `ArgumentNullException` with `ParamName = "source"`.

The author had already guarded the sibling pattern in the same file — `StockTransferRequestPackingIntegration.cs:145` reads `nsdto is null ? null : MapInfoDto(nsdto)`. Line 170 was missed.

### Why it was invisible

`PostTransferOrderIFCmd.cs:58-62` catches everything and returns only `ex.Message`; `TransferOrderItemView.razor.cs:217` toasts `result.ErrorMessage`. The stack trace, the exception type and the identity of the failing lookup were all discarded, so an infrastructure hiccup presented to the operator as an unactionable LINQ error with **no indication of whether anything had been written to NetSuite**.

### Field outcome

Nothing was lost. The exception is thrown at `PostTransferOrderIFCmd.cs:26`, **before** `SaveTOItemFulfillment` at `:49`, so no fulfillment was posted. The order shipped cleanly on the next attempt.

Confirmed in NetSuite afterwards: `status = F` (partially fulfilled), `ordpicked = T`, 900 units across 11 items with every `quantitypacked` exactly equal to its ordered quantity — no double-posting. The empty item grid seen afterwards was correct rendering, not a second fault: `TransactionVM.cs:48` defines `NSLineQuantityPacked` as *open* quantity, and once fully packed it is `0`, so the GOOD tab filter (`x.NSLineQuantityPacked != 0`) matches nothing.

### The fix

Six call sites — the trip-ticket lookup and the header lookup in each of the three packing integrations:

| File | Header | Trip ticket |
|---|---|---|
| `StockTransferRequestPackingIntegration.cs` | `:143` | `:170` |
| `ReturnPackingIntegration.cs` | `:83` | `:110` |
| `VendorReturnAuthorizationPackingIntegration.cs` | `:83` | `:110` |

```diff
- var row = response.items.FirstOrDefault();
+ var row = response.items?.FirstOrDefault();
```

This restores the behaviour the author intended rather than adding new logic: a null result already returned `null`, which all three command handlers already treat as **unresolved** and fail closed on with an operator-actionable message:

> Could not verify trip-ticket exemption for transfer order X. **Nothing was saved.** Please reload the order and try again.

No posting behaviour changes; the only difference is a reportable message instead of a crash.

### Tests

`PackingNullItemsTests` (new, 8 cases) drives `CapturingNetSuiteClient.NextNullItems` — a new flag that reproduces a 2xx body omitting `items` — and asserts each of the six lookups returns `null` rather than throwing. Two further cases prove the guard does not swallow real answers: a present exemption row resolves `true`, and `ExemptionId = 0` resolves `false` (not `null`).

The regression test was validated by reverting the `:170` guard, confirming exactly one test fails, then restoring it.

Build: `Integration.NS`, `Web.BlazorServer`, `Api.CoreWebAPI` → **0 errors**. Tests: **340/340 passed**.

> Flakiness note (unchanged): `GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks` and `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default` fail intermittently under parallel load and pass in isolation and on clean re-runs. Both pre-date this work.

---

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Select the raw category id, not `BUILTIN.DF`** (chosen) | *Display name*: the guard compares against ids; names would need fragile string matching and break on renaming. |
| **Fix Returns only; defer STR** (chosen) | *Add the STR guard now*: it would hard-block STR packing, because `NS_TO_x_Packing_Get_Items.sql` has the identical un-projected column. The guard and the projection must ship together. |
| **Add the property to both `ReturnsLineDTO` types** (chosen) | *Only the grid type*: `GetReturnsLineQry` uses `Commons.NS`, the grid uses `Packing.Returns`. Leaving one inconsistent re-creates the exact drift that caused this. |
| **Null-guard the six packing sites, not all 23** (chosen) | *Repo-wide sweep*: correct eventually, but mixing a broad refactor with two behavioural fixes widens the review surface. Follow-up #1. |
| **Return `null` (fail closed), not default to non-exempt** (chosen) | *Default to `false`*: would post a Packed ship status for an exempt order, stranding shipped goods — the exact harm the fail-closed design was written to prevent. |

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Null-guard the remaining 17 `response.items.FirstOrDefault()` sites outside packing | Open — same defect family |
| 2 | Project `custbody_dbti_transfer_category` in `NS_TO_x_Packing_Get_Items.sql` **and** add the matching guard to `PostTransferOrderIFCmd` — together | Open — must ship as a pair |
| 3 | Stop discarding diagnostics: return the exception type alongside `ex.Message` in the three `Post*IFCmd` handlers | Open — recommended; this defect cost ~20 minutes of investigation purely for want of context |
| 4 | Review receiving-side guards for the same `TransferCategory` gap | Open |
| 5 | Confirm in NetSuite that the `InventoryDetail` lines for a packed order carry the expected bins and inventory statuses (validates the `InventoryStatusMap` fix end to end) | Pending |
| 6 | `graphify update .` (blocked by sandbox bash rules) | Outstanding |

## Validation Limits

- **No live NetSuite verification.** Both fixes are derived from source plus the field evidence already recorded; SuiteQL behaviour for the newly projected column is unproven against the account. It mirrors a binding that already works in production (`StockTransferRequestIntegration.cs:253`), but should be confirmed on a Returns order.
- **Defect B's root cause is inferred, not proven.** It fits every observation — message, timing, successful retry, no data loss — and `:170` is the only unguarded null-collection LINQ on that save path, but no stack trace was captured. The fix is correct regardless of which site threw, since all six were guarded identically.
- **Why NetSuite returned a body without `items` is unknown.** Rate limiting and transient SuiteQL faults are plausible but unconfirmed. The guards make the symptom survivable; they do not explain the upstream cause.
- **The un-`DF`'d column binding to `int` is assumed, not tested.** If SuiteQL returns the reference as a JSON string, deserialization will fail loudly on the first Returns load — a fast, visible failure, not a silent one.
- **The Returns blast radius was not enumerated.** Orders saved before `3ed7f153` may have been mis-stated as Good stock; whether any Returns-Bad order was affected is unknown and may warrant an inventory review.