# Fix: Mobile TO packing — BackOrdered tab shows nothing

Date: 2026-10-20
Status: Fix in source, build clean — **awaiting client verification on the affected transfer order**
Mode: Bugfix / RCA
Scope: Mobile — Packing → Transfer Order item view (`Mobile.MAUI/Components/Pages/Packing/TransferOrder`)
Related:
- `cb4b0987` (2026-10-16) — introduced the `Done` tab and the `IsCompleted` split that caused this regression
- `ec7795af` (2026-07-24) — introduced the `BackOrdered` tab and its original `GoodTOItems` feed
- [2026-10-19 web STR packing closed-line guard](2026-10-19-web-str-packing-closed-line-guard-missing-fix.md) — the server-side sibling: it dropped the backorder term from the open-quantity formula
- `a39f1ba9` (2026-10-20) — returns-side guard that reads `LineQuantityBackOrdered` (see Follow-up 1)

Changed files:
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor` — tab data source, stray `)` removed
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs` — `BackOrderedTOItems`, `FilteredBackOrderedTOItems`, flag, `Done` filter, search wiring

## Summary

The `BackOrdered` tab on a transfer order showed no rows — and in most cases the tab itself was absent, because it renders behind `@if (HasBackorderItems)`.

The tab was added on 2026-07-24 reading `GoodTOItems.Where(x => x.LineQuantityBackOrdered > 0)`. On 2026-10-16 the closed-line work split the response into `GoodTOItems` (open, scannable) and `DoneTOItems` (closed, read-only), and `GoodTOItems` stopped being "every line". The BackOrdered tab was left pointing at `GoodTOItems`.

NetSuite closes a line when it backorders the remainder, so the lines the tab exists to show are exactly the lines that moved into `Done`. The tab's feed became empty for the only rows it was built to display, and `HasBackorderItems` — which read the same list — turned the tab off entirely.

## Root Cause

Two changes compose into the defect.

**1. The feed narrowed (`cb4b0987`).**

```csharp
var source = result.Data ?? [];
var openLines = source.Where(line => !line.IsCompleted).ToList();

GoodTOItems  = openLines.Select(...)                      // ← no longer "all lines"
DoneTOItems  = source.Where(line => line.IsCompleted)     // ← new bucket
```

`IsCompleted` is `IsLineClosed || NSLineQuantityRemaining <= 0` (`TransferOrderLineVM.cs`), and `IsLineClosed` is `IsClosed == "T"` from `tl.isclosed`.

**2. NetSuite closes a line whose remainder is backordered.** The backordered quantity stays on the line (`tl.quantitybackordered`), `tl.isclosed` goes to `T`, and the line therefore satisfies `IsCompleted`. It is routed to `DoneTOItems`, not `GoodTOItems`.

The tab and its visibility flag both read `GoodTOItems`:

```csharp
// before
bool HasBackorderItems => GoodTOItems.Any(x => x.LineQuantityBackOrdered > 0);
```
```razor
<RadzenDataList TItem="TransferOrderLineVM"
                Data="GoodTOItems.Where(x => x.LineQuantityBackOrdered > 0)">
```

With the backordered lines in `DoneTOItems`, the filter matches nothing and `HasBackorderItems` is `false` — the tab disappears rather than rendering empty. No error is raised anywhere, which is why the symptom reads as "the tab is not functioning".

The `Done` tab also rendered these rows under a green **"Fully shipped"** badge (`TransferOrderItemView.razor:159`), which is wrong for a line that still has a backorder.

### Confirmation query (not yet run)

The premise — `isclosed = T` together with a non-zero backorder on the same line — is the documented NetSuite behaviour for a backordered remainder, but it was **not observed on a live transaction in this session**. Confirm on the order the operator tested:

```sql
SELECT tl.id AS line, tl.isclosed, tl.quantity, tl.quantitypacked,
       tl.quantitybackordered, tl.quantityshiprecv
FROM transaction t
JOIN transactionline tl ON tl.transaction = t.id
WHERE t.tranid = 'ISTR-…' AND tl.transactionlinetype = 'SHIPPING'
ORDER BY tl.linesequencenumber
```

A row with `isclosed = 'T'` and `quantitybackordered > 0` confirms this analysis. If no such row exists, the defect is elsewhere and this document should be revisited.

## How it was found (investigation path)

1. **Located the only two BackOrdered tabs in the mobile app** — `Packing/TransferOrder` and `Packing/Returns`. No `backlog` identifiers exist; the branch name is not a code artefact.
2. **Read the tab's data binding and its visibility flag** — both `GoodTOItems`.
3. **Blamed the feed, not the tab.** `git log -G"QuantityBackOrdered"` showed the feed was changed by `cb4b0987` (2026-10-16), the closed-line commit, which added `DoneTOItems` and the `!IsCompleted` split in the same edit.
4. **Checked the data path was intact** — `NS_TO_x_Packing_Get_Items.sql:42` still projects `tl.quantitybackordered AS LineQuantityBackOrdered`; `TransactionDTO`, `TransferOrderLineDTO`, `TransactionVM` and `TransferOrderLineVM` all declare it; the mobile endpoint applies no renaming. The quantity was arriving; it was being filtered away client-side.
5. **Eliminated the neighbouring suspects** — `9ac3fa3c` / `06a3cc7b` (last-scanned panel) only touched visibility of a floating panel; `86d253a8` (2026-10-19) changed the *web* open-quantity formula, not the mobile list; `a39f1ba9` (2026-10-20) touched the returns posting path.
6. **Client confirmation** — the reporting screen is Transfer Order, not Returns.

## The Fix

Five changes in one view; no server, DTO, or SQL change.

### 1. A dedicated list, built from the raw response

```csharp
// Backordered lines are built from the raw response, not from GoodTOItems.
// NetSuite closes a line when it backorders the remainder, so a backordered
// line is also IsCompleted and would otherwise land in DoneTOItems and vanish
// from the BackOrdered tab.
List<TransferOrderLineVM> BackOrderedTOItems = [];
```

```csharp
var source = result.Data ?? [];
var openLines = source.Where(line => !line.IsCompleted).ToList();

BackOrderedTOItems = source.Where(line => line.LineQuantityBackOrdered > 0).ToList();
```

Built from `source`, so it is independent of the Good/Done split. No re-mapping is needed: the tab renders only `MaterialName`, `LocationName`, `UoMName` and `LineQuantityBackOrdered`, all present on the deserialised line.

### 2. Visibility reads the new list

```csharp
- return GoodTOItems.Any(x => x.LineQuantityBackOrdered > 0);
+ return BackOrderedTOItems.Count > 0;
```

### 3. `Done` no longer claims a backordered line is fully shipped

```csharp
- DoneTOItems = source.Where(line => line.IsCompleted)…
+ DoneTOItems = source.Where(line => line.IsCompleted && line.LineQuantityBackOrdered <= 0)…
```

A closed-with-backorder line now appears in exactly one bucket — BackOrdered — instead of appearing in Done under a false badge.

### 4. Search applies to the tab

`FilteredBackOrderedTOItems` is populated in both branches of `ApplySearch` and bound in the razor:

```razor
- Data="GoodTOItems.Where(x => x.LineQuantityBackOrdered > 0)"
+ Data="FilteredBackOrderedTOItems"
```

This closes a second, smaller defect: the tab previously bypassed the search box while every other tab honoured it. Fixing the feed and wiring search are the same edit.

### 5. Stray `)` removed

`TransferOrderItemView.razor:180` read `class="rz-p-2") Orientation=…` — the leftover of a deleted inline expression. It parsed as a junk attribute, so it never failed, but it hid the next real error in that element. (The identical defect remains at `ReturnsItemView.razor:108`; left untouched — different screen.)

## Behaviour Matrix (after the fix)

| Line state (`isclosed` / backordered / open qty) | Good | Bad | Done | BackOrdered | Tab visible |
|---|---|---|---|---|---|
| Open, no backorder | yes | yes | no | no | no |
| Open, `backordered > 0` | yes | yes | no | **yes** | yes |
| Closed (`T`), `backordered > 0` | no | no | no | **yes** | yes |
| Closed (`T`), no backorder | no | no | yes | no | no |
| Fully shipped, `backordered = 0` | no | no | yes | no | no |

Invariant: a backordered line is always reachable and always shown once, in `BackOrdered`, regardless of `isclosed`. No line is both in `Done` and `BackOrdered`.

Note that "Open, `backordered > 0`" appears in both **Good** and **BackOrdered** — unchanged from the original design, and deliberate: the open remainder is still scannable while the backordered quantity is being reported.

## Build and graph

- `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android` → **0 errors**, 920 warnings (all pre-existing; none in the changed files).
- `graphify update .` → `18916 nodes, 44486 edges, 620 communities`.
- No automated test added: the only test project in the solution is `Integration.NS.Tests`, and it does not cover the MAUI Blazor views. Verification is on-device.

## Verification Plan

1. **The affected transfer order.** Open Packing → Transfer Order → the order. The BackOrdered tab must be present and list every line with `quantitybackordered > 0`, including the ones that are closed in NetSuite.
2. **`Done` must not contain them.** Confirm the backordered rows are absent from `Done` and no longer carry the "Fully shipped" badge.
3. **A fully shipped order (no backorder).** `Done` behaves exactly as before; the BackOrdered tab stays hidden.
4. **An open order with a partial backorder.** The line appears in `Good` (still scannable) *and* `BackOrdered`.
5. **Search.** Typing a material code filters the BackOrdered tab like the other tabs.
6. **Save.** Scan and save an order containing a backordered line; the post must still succeed and the normal save path must be unaffected — this change touches no payload code.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Build the tab's list from `source`** (chosen) | *Read `GoodTOItems ∪ DoneTOItems`*: works, but couples the tab to two lists that exist for a different purpose (scannability) and breaks again the next time a bucket is added. |
| **Exclude backordered rows from `Done`** (chosen) | *Leave `Done` untouched and show the row in both tabs*: the `Done` badge says "Fully shipped", which is false when a backorder exists, and would send the operator to NetSuite to reconcile a line the app called complete. |
| **Bind the tab to a filtered list** (chosen) | *Keep the inline `Where`*: that is what silently bypassed search, and an inline predicate cannot be reused by `HasBackorderItems`. |
| **Fix on the client only** (chosen) | *Filter in `NS_TO_x_Packing_Get_Items.sql`*: the same script feeds the desktop STR grid and the save guard; removing rows there would silently drop lines from those consumers. |
| **Leave the Returns screen alone in this change** (chosen) | It has an independent, provable defect (Follow-up 1). Mixing screens would blur the verification of a fix the client reported against Transfer Order only. |

## Residual Risk

1. **Medium — the premise is inferred, not observed.** If no line on the tested order carries `isclosed = T` *and* `quantitybackordered > 0`, the tab's emptiness has another cause and this fix will not restore it. The confirmation query above settles it; run it before signing off.
2. **Low — `BackOrderedTOItems` holds the deserialised line, not a re-mapped copy.** Correct today because the tab is read-only. If scan fields are ever added to this tab, it must switch to a mapped list like `GoodTOItems`, or it will mutate state the save path does not see.
3. **Low — no refresh path.** `ActionGetTOItems` runs on initialization only, so a screen left open while the order is backordered elsewhere will not show the tab until it is reopened. Pre-existing; unchanged by this fix.
4. **Low — no automated coverage.** A regression in this view would be caught only on-device.
5. **Low — the `Returns` screen still has the same class of defect.** See Follow-up 1; not fixed here.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | `Packing/Returns` — `SaveScan` rebuilds `ReturnsItems` without copying `LineQuantityBackOrdered` and prunes rows where `NSLineQuantityPacked == 0`, so its BackOrdered tab empties after one save and the `a39f1ba9` over-receipt guard receives `0`. Build the post payload separately instead of mutating the UI list. | Open — recommended next |
| 2 | Remove the same stray `)` at `ReturnsItemView.razor:108` while touching that screen | Open |
| 3 | Run the confirmation SuiteQL on the affected transfer order and record the result here | Open — client |
| 4 | On-device verification, items 1–6 of the Verification Plan | Open — client |
| 5 | Consider a `Mobile.MAUI` test project, or extract the bucket split into a testable pure function, since three of the last five mobile defects were list-partitioning bugs | Open — proposal |

## Validation Limits

- **No live NetSuite verification.** The mechanism (closed + backordered on the same line) rests on the team's documented STR behaviour and the symptom reported by the client, not on data I could query.
- **No on-device run.** Correctness of the binding is asserted from the compile and the data flow, not from a rendered screen.
- **The "Fully shipped" badge removal is a side effect.** It is the correct outcome, but it changes what a closed-with-backorder line looks like in `Done` — worth explicit confirmation by the operator.
