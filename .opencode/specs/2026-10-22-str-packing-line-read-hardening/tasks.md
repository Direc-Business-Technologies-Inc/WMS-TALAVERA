# Tasks — STR packing line read: completeness, vendor scope, gate stability

**Date:** 2026-10-22
**Status:** Confirmed
**Mode:** BUGFIX
**Spec:** `design.md` (sibling); requirements in `requirements.md`
**Goal:** stop silently dropping and duplicating packing lines, make the vendor gate correct at 1,000+ lines, and cut per-scan transfer cost so the gate is not slower than the flow it replaced.

## Architecture summary

One shared SQL script gains a stable sort, pre-aggregated bin joins, and an
optional call-scoped vendor predicate. One MediatR handler owns pagination so
every caller inherits it. The vendor resolver switches from a full line read to
a small candidate-id query. The mobile screen gains a resolve indicator and a
three-way Change Vendor prompt. The save path, the web packing flow's behaviour,
receiving, and the database schema are untouched.

## Verified technical context

- .NET 8; projects per `WMS.slnx` (Domain, Application.DTOs, Application.UseCases, Shared.*, Database.*, Integration.NS, Mobile.MAUI, Web.BlazorServer, Api.CoreWebAPI, Integration.NS.Tests).
- **`.claude/` guides do not exist in this repository** and `CLAUDE.md` is stale (describes an LSMS/SAP solution). Guidance here is drawn from verified code and `CLAUDE.md`'s branch rule only. Do not invent repository rules from the missing guides.
- Measured facts (2026-10-22, SuiteQL console): sample STR has **1,657** SHIPPING lines; joined rows **1,661** vs **1,657** distinct; **95** distinct vendor ids.

## Global constraints

1. Create the branch before any edit: `claude/fix/str-packing-line-read` (`CLAUDE.md:33`).
2. Do not modify: `SaveTOItemFulfillment`, `PostTransferOrderIFCmd`, payload DTOs, the Receiving Transfer-Order handler/script, or any `.razor` outside the packing Transfer-Order screen.
3. **No `--` or `/* */` comments inside `.sql` files.** The statement is whitespace-collapsed before sending; comments have shipped broken scripts before (`SuiteQLText.cs:92-114`).
4. SuiteQL constructs that are **proven to fail on this account** and must not be introduced: `BUILTIN.DF` inside `GROUP BY`; `ORDER BY <aggregate expression>`.
5. Keep `tranid` on the `@key` form (quoted, escaped). Only numeric/scoped values use the `{key}` form.
6. `dotnet build` from the solution root must be clean and `dotnet test Integration.NS.Tests` green at every checkpoint.

---

## Phase A — Data correctness

### T1. Fix the join fan-out
**File:** `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql:118-142`
**Current:** four bare `LEFT JOIN inventorybalance` (`ibv1`, `ibv2`, `ibi1`, `ibi2`) → multiple rows per line (measured 1,661 vs 1,657).
**Target:** replace each with a `SUM(quantityavailable)` derived table grouped by `(item, location, binnumber, inventorystatus)`, joined on the same keys, mirroring `ibli1`/`ibli2` (`:144-186`). Do not use `SELECT DISTINCT` — the duplicate rows differ in quantity columns and would survive it.
**Acceptance:** the fan-out comparison returns `JoinedRows == DistinctLines` for the sample tranid.

_Requirements: 1.3_

### T2. Add a stable total order
**File:** same script, after the WHERE block (`:188-193`)
**Target:** `ORDER BY tl.id`.
**Acceptance:** two consecutive executions return identical row order; required before T3.

_Requirements: 1.2_

### T3. Page the line read in the handler
**File:** `Application.UseCases/Queries/Transaction/Packing/NS/TransferOrder/GetTransferOrderLineQry.cs:19-38`
**Current:** one `NetsuiteQuery` call with no limit; `CancellationToken` not forwarded.
**Target:**
- Extract a private paged-read method: `limit = 1000`, `offset += 1000`, stop when a batch is shorter than 1,000, concatenate results.
- Guard against a runaway loop: cap at 100 pages and fail loudly on overflow.
- Forward `cancellationToken` to `NetsuiteQuery`.
- Treat a `null` result as an empty batch.
- Any page failure propagates as a failure; never return a partial set.
**Acceptance:** the sample tranid returns 1,657 rows; a ≤1,000-line document issues one NetSuite call; a 2,000-line document issues three (one extra empty page at exact multiples is expected).

_Requirements: 1.1, 1.2, 1.4, 1.5, 1.8_

### T4. Paging tests
**File:** new file under `Integration.NS.Tests/Implementations/Transactions/Packing/`
**Target:** `[Theory]` over 0 / 999 / 1,000 / 1,001 / 2,000 rows with a fake returning synthetic pages. Assert concatenation equals the reference set, order is preserved, no duplicates, and the number of calls is as expected. Add a page-failure case and a page-cap-overflow case, both asserting failure rather than partial success.

_Requirements: 1.1, 1.5, 1.8_

> **Checkpoint A** — fan-out clean; 1,657 rows returned; new tests and the existing suite green. Do not proceed on a partial pass.

## Phase B — Vendor scope in SQL

### T5. Add the call-scoped vendor predicate
**File:** `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql`, WHERE block (`:188-193`)
**Target:** `AND ({vendorId} = 0 OR tl.custcol_dbti_vendor = {vendorId})`.
**Why this form:** `@key` is spliced as a quoted string (`SuiteQLText.cs:52`) and would compare a string against a numeric List/Record field; `{key}` is the raw-integer-splice form validated by `IntegerList` (`:69-89`), and `0` passes that guard.
**Acceptance:** run the edited script in the SuiteQL console with `{vendorId}` replaced by `0` → row count equals the tranid's `COUNT(*)`; replace with a real id → only that vendor's lines. A non-integer value must throw locally, never reaching NetSuite.

_Requirements: 1.6, 1.7_

### T6. Always supply the scope; remove the in-memory filter
**File:** `Application.UseCases/Queries/Transaction/Packing/NS/TransferOrder/GetTransferOrderLineQry.cs:19-38`
**Target:**
- Parameter dictionary becomes `{ "tranid": ..., "vendorId": request.order.VendorId is > 0 ? request.order.VendorId!.Value.ToString() : "0" }`.
- Delete the in-memory vendor filter (`:31-36`); the SQL predicate is authoritative. Keeping both would mask a broken predicate.
**Acceptance:** web path (no `VendorId`) receives 1,657 rows; scoped path receives that vendor's line count.

_Requirements: 1.6, 1.7_

### T7. Rework the filter-shape test
**File:** `Integration.NS.Tests/Implementations/Transactions/Packing/StrPackingVendorFilterShapeTests.cs`
**Current premise (now inverted):** "the vendor never travels into the script parameters."
**Target:** assert `tranid` travels as `@tranid`; the vendor travels as `{vendorId}`; an unscoped call sends `"0"`; a scoped call sends the id. Update the file's doc comment, which still claims the barcode carries the internal id.

_Requirements: 1.4, 1.6, 1.7, 4.2_

> **Checkpoint B** — Postman: unscoped `/Items` returns 1,657; scoped returns the vendor slice; tests green.

## Phase C — Vendor resolver

### T8. Add the candidate query
**File:** new `Integration.NS/NSScripts/NS_TO_x_Get_Vendor_Candidates.sql`
**Target:** exactly the script in `design.md` — `SELECT DISTINCT tl.custcol_dbti_vendor AS Id` with the `tranid` / `recordtype` / `SHIPPING` filters. **No** `GROUP BY`, **no** `BUILTIN.DF`, **no** `ORDER BY` on an aggregate. Deserialize into `VendorLookupDTO` (only `Id` is populated here).
**Acceptance:** console run for the sample tranid returns 95 rows.

_Requirements: 2.1, 2.2_

### T9. Point the resolver at it
**File:** `Application.UseCases/Queries/Transaction/Packing/NS/TransferOrder/ResolveTransferOrderVendorQry.cs:34-66`
**Target:** replace the `GetTransferOrderLineQry` call (`:39-45`) with the candidate query; keep `GetVendorsByIdsAsync` enrichment (`:57-59`); wrap the handler body in `try/catch` returning `ApiResult<VendorLookupDTO>.ServerError(...)`.
**Acceptance:** a logged resolve shows one candidate query plus one vendor-record read, and no full line query.

_Requirements: 2.1, 2.2, 2.4_

### T10. Change the matcher contract and migrate its tests
**File:** `ResolveTransferOrderVendorQry.cs:74-225` (matcher) and `Integration.NS.Tests/.../TransferOrderVendorResolverTests.cs`
**Target:** `Resolve(candidates, vendors, scannedVendor, orderNumber)` — candidate ids seed the name map; names come from the vendor records (`entityid` / `companyname` / `fullname`). Matching, normalization, ambiguity, and message rules are unchanged. Migrate the 12 existing cases; add one asserting a vendor whose lines sit beyond row 1,000 still resolves.

_Requirements: 2.1_

### T11. Make the no-vendor message actionable
**File:** `ResolveTransferOrderVendorQry.cs:108-112`
**Target:** state the route, e.g. "… has no vendor on its lines. Fulfil this order from web Packing instead."

_Requirements: 2.3_

## Phase D — Mobile UX

### T12. Resolve indicator
**Files:** `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor` (gate card), `.razor.cs`
**Target:** while `_resolvingVendor`, render an inline spinner plus "Resolving vendor…" on the gate card; render the vendor badge as soon as the id/name are known, before lines arrive. Do **not** wrap this in `<Loading>` — it hides `ChildContent` (`Loading.razor:17`) and would blank the header and back button.

_Requirements: 3.1, 3.2_

### T13. Change Vendor prompt
**File:** `.razor.cs:539-546`
**Target:** when `ScanCount > 0`, confirm "Save first?"; on yes, run `ActionSaveScan` and switch **only if `Result.Success`**; on no, confirm "Discard N scans?" then discard or cancel. `ExecuteAppActionAsync` absorbs exceptions into `Result` (`ActionFactoryService.cs:57-69`) — it does not throw, so branch on the result, not on a try/catch.
**Acceptance:** four paths verified — save succeeds (gate switches), save fails (gate and scans retained), discard (cleared), cancel (unchanged).

_Requirements: 3.3, 3.4, 3.5_

### T14. Record the busy-flag invariant
**File:** `.razor.cs:79-84`
**Target:** extend the comment to say *why* the flag must span the follow-up item load — a second resolution cannot start until the first load has applied, otherwise two vendors' line sets can interleave.

_Requirements: 5.3_

## Phase E — Documentation and verification

### T15. Correct the change document
**File:** `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md`
**Target:** correct §4 — mobile posts only scanned lines; the zero-quantity filler behaviour belongs to the web handler. Record the 1,000-row truncation, the fan-out finding, the 95-vendor measurement, and the `{vendorId}` decision. Ensure the barcode is described as the vendor name everywhere.

_Requirements: 5.1, 5.2_

### T16. Final verification
**Target and acceptance:**
1. Fan-out comparison: `JoinedRows == DistinctLines`; count query returns 1,657.
2. Candidate query: 95 ids for the sample tranid.
3. Postman: unscoped `/Items` → 1,657 rows; scoped `/Items` → the vendor slice. Record both timings in the change document.
4. Web packing remains ungated and still fulfils the whole order.
5. Receiving, trip ticket, returns, VRA: no file touched; build and existing tests unchanged.
6. Null-vendor documents: behaviour unchanged (still out of handheld scope).
7. `dotnet build` all projects; `dotnet test Integration.NS.Tests` green.
8. Record that no device verification was possible and carry it as an open risk.

_Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

---

## Deferred / optional (not MVP)

- `Optional` — timing records and text-file export of request/response data (deferred at the user's request).
- Determinate progress bar — dropped; spinner retained.
- In-memory retention of scanned lines across vendor switches — not needed; per-vendor bursts average ~17 lines.
- Ungated fallback for null-vendor documents — accepted risk.
- Merged multi-vendor save payloads — rejected; incompatible with the single-payload save and its `mixedBinFlags` guard.
- Vendor-scoped read as a **separate** script — unnecessary; the `{vendorId}` predicate avoids duplication.

## Commands

```text
dotnet build                      # solution root
dotnet test Integration.NS.Tests  # unit/regression suite
```

Do not create branches, commits, or pushes as part of writing this specification;
`CLAUDE.md` governs branch and commit behaviour at implementation time.
