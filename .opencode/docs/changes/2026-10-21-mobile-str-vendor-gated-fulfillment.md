# Mobile STR fulfillment — vendor-gated by barcode

Date: 2026-10-21
Area: `Mobile.MAUI` (Packing → Transfer Order), `Api.CoreWebAPI`, `Application.UseCases`, `Integration.NS/NSScripts`
Type: feature — handheld workflow change

## Context

The printed Stock Transfer Request groups its items by vendor: one page carries one
vendor's items, its own totals, `****Nothing Follows****`, and a `Vendor:` block.
The vendor name in that block is the **line vendor** (`custcol_dbti_vendor`), not the
item-master preferred vendor and not the STR header (a TO/ITO has no header vendor —
see `.opencode/specs/2026-10-06-preferred-vendor-per-line/requirements.md:85-89`).

Operators pick one vendor's section at a time, but the handheld loaded **every** line
on the STR. Nothing stopped a scan against another vendor's section, and a mis-scan
was only caught by quantity guards, not by vendor.

## Hardening follow-up (2026-10-22)

The vendor gate read the same line query the screen uses, and that read had three
defects that the gate made worse. Measured on the sample STR (SuiteQL console):

| Finding | Measurement |
|---|---|
| NetSuite's SuiteQL page cap silently truncated the read | **1,657** SHIPPING lines on the document; at most **1,000** were transferred |
| Four bare `inventorybalance` joins fanned one line into several rows | **1,661** joined rows for **1,657** distinct lines (4 duplicates) |
| The gate read all lines twice per scan to use one vendor's slice | **95** distinct vendors on the document; ~17 lines per vendor |

The fix (spec `.opencode/specs/2026-10-22-str-packing-line-read-hardening/`) lands in
three parts:

1. **Paging.** `MGetTransferOrderLineQryHandler` loops 1,000-row pages through
   `NetsuiteQueryWithMetadata`, orders the script by `tl.id` for a stable total order,
   stops on the authoritative `totalResults` (never probing past the end, which
   NetSuite answers with 404), caps at 100 pages, and fails rather than returning a
   short set.
2. **Row uniqueness.** The four `inventorybalance` joins are pre-aggregated with
   `SUM(...) GROUP BY (item, location, binnumber, inventorystatus)`, mirroring the
   location-level joins, so a line is returned exactly once.
3. **Vendor scope in SQL.** The line script takes the `{vendorId}` predicate above and
   the resolver reads vendor ids from `NS_TO_x_Get_Vendor_Candidates.sql` instead of a
   full line read. Duplicate rows were also a real over-scan risk in the UI, which is
   not deduped; that risk is removed at the source.

## What is printed on the document

Two barcodes, both owned by NetSuite scriptlet **1671** (`NetSuitePrint.cs`), not by
this repo:

1. **Page footer** — the document number verbatim, e.g. `ISTR-THM00100000776-2`.
   Some documents carry a suffix in the `tranid` itself; most do not. It is never a
   page or vendor index, and the WMS does not parse it.
2. **Under the Vendor field** — the vendor **name** as printed text
   (`ISABELA GIOS COMMERCIAL, INC.`). It is **not** the internal id: confirmed on the
   handheld on 2026-10-21, where the scan returned the name and the first
   implementation rejected it as "not a vendor barcode".

No new NetSuite field, no lookup table, no new custom record. The name is resolved
against the vendors already present on the transfer order.

## New workflow

1. Packing list (`/packing`) — operator scans barcode #1. The existing handler puts
   the tranid into the list search, narrowing the list to that transfer order.
2. Operator opens the order (`/packing/transferorder/{OrderNumber}`).
3. **Vendor gate.** The item lists stay empty and a vendor selection card is
   shown. Every scan is treated as a vendor scan, regardless of the Good/Bad toggle.
4. Operator selects a vendor from the dropdown (loaded from **this order's** line
   vendors via `POST /Packing/TransferOrder/Vendors`) or scans barcode #2. Scans are
   resolved by `POST /Packing/TransferOrder/ResolveVendor` against the vendors on
   **this order's** lines and answer `{ Id, Name }`.
5. Lines are loaded filtered to the resolved id; the header shows the canonical
   name plus a **Change Vendor** action.
6. Operator scans items into Good/Bad as before and saves. Only the scanned
   vendor's lines carry quantities.

Changing vendor discards the **unsaved** scans and reloads the list for the newly
scanned vendor. It deliberately does not attempt to merge two vendors' scans into one
payload.

## Design decisions

### 1. The filter key is the line vendor, not the bin-assignment vendor

Two vendors exist in the data:

| Source | Meaning | Used for |
|---|---|---|
| `transactionline.custcol_dbti_vendor` | who the line was requested from; printed on the paper; editable per line | **this filter** |
| `itemvendor.preferredvendor = 'T'` | whose bin the item is stored in; item-master data; not on the paper | existing bin resolution, unchanged |

`.opencode/specs/2026-10-06-preferred-vendor-per-line/requirements.md:109-110` (3.3)
freezes the bin-assignment side. The two agree on every line whose vendor was not
manually overridden; they only diverge on an override, where the displayed bin may
belong to the item-master vendor. That divergence is pre-existing and is **not**
changed here.

### 2. The barcode is a name — resolve the name, then use the id

The scan is text, so it cannot be used as a filter key directly. Resolution is
`scanned name → vendor on this document → internal id`, in that order:

1. Run one small candidate query (`NS_TO_x_Get_Vendor_Candidates.sql`) that returns
   the distinct `tl.custcol_dbti_vendor` ids for the tranid, with the same
   document-level filters as the line read and `IS NOT NULL`. It deliberately does
   not read the line values or names (design D4a).
2. Read those vendor records for `entityid`, `companyname`, and `fullname`.
3. Match the scan against those record names — whitespace/case-insensitive first,
   then a punctuation-insensitive retry (printed labels and record names can differ
   by a comma or a period).
4. Return the matched vendor's id and the name that matched.

The candidate read is independent of the line read's 1,000-row page, so a vendor
whose lines sit beyond row 1,000 still resolves. Names come from the vendor record,
not from the line's `BUILTIN.DF` text: that display text is not guaranteed to be
the name the print template rendered, and the first field attempt (2026-10-21)
resolved to nothing until the vendor record's own name fields were added.

Why scope the vendor read to the document's ids rather than a global name lookup:

- Only vendors already on the order are candidates, so a duplicate display name
  elsewhere in the vendor list cannot make the scan ambiguous.
- It uses the same record the print template read from, so one of `entityid` /
  `companyname` / `fullname` matches by construction.
- The extra read is one small `id IN (...)` query, not a scan of the vendor table.

A scan that matches nothing is rejected with the scanned text in the message. A scan
that matches more than one vendor (possible only after the punctuation-insensitive
retry) is rejected rather than guessed. A document with no vendor on any line names
web Packing as the alternative route.

### 3. The scope lives in the SQL script with a call-scoped predicate

`NS_TO_x_Packing_Get_Items.sql` is shared by the mobile line list, the web packing
grid (`StockTransferRequestPackingIntegration`), and the save-path re-read. All of
them send a `vendorId` parameter:

- `AND ({vendorId} = 0 OR tl.custcol_dbti_vendor = {vendorId})` in the `WHERE` block.
- `{key}` is the repo's raw-integer splice, validated by `SuiteQLText.IntegerList`,
  so a non-integer throws locally before reaching NetSuite. `@key` would splice a
  quoted string and compare text against a numeric List/Record field.
- `0` is the "no scope" sentinel. Mobile sends the resolved id; every other caller
  sends `0` and receives the full row set, so the predicate's default reproduces the
  previous behaviour. No in-memory filter remains in the handler.

### 4. The save logic is untouched

`PostTransferOrderIFCmd` / `SaveTOItemFulfillment` are unchanged. Mobile posts **only
the lines the operator scanned**; it does not send zero-quantity entries for a
vendor's unsubmitted lines. The zero-quantity filler behaviour belongs to the web
flow: `ItemReceiptPackingHandler.PostItemFulfillment` rebuilds its payload from all
non-completed source lines and posts a zero-quantity entry for each one it was not
asked to fulfil. That is why the web caller must keep receiving the full line set
(`vendorId = 0`) and why the new GitHub-hardening work leaves the save payload alone.

### 5. Hard gate, not a soft filter

Lines remain hidden until a vendor barcode is scanned. A soft filter (show everything,
filter when a scan happens) would let the operator scan items before the gate is
resolved and defeat the control.

### 5b. Vendor resolution is serialized with a busy guard

`HandleItemScan` is an `async void` event handler, and the vendor path inside it
awaits a server round trip (the order's lines plus the vendor records). Handheld
scanners double-fire and operators re-scan when nothing seems to happen, so two
`ResolveVendorScan` calls can overlap — and their `ClearVendorLines` → set id →
load-sequences can interleave: the slower call's clear wipes the faster call's
loaded lists, or the slower call's id silently replaces the faster one's, leaving
the operator scanning against a vendor they did not resolve — the exact
mis-fulfillment this feature prevents.

A `_resolvingVendor` flag serializes it. An arriving scan while the flag is set is
swallowed, not queued — whatever the scanner emits during resolution (a duplicate
of the same barcode, or an eager item scan) is noise, and the operator's next real
scan is handled normally once the gate resolves. The flag is held across the
follow-up item load too: scans of a still-list-less screen can only produce
"Item not found" warnings.

### 6. Mobile only — the web packing flow is intentionally unchanged

This is a decision, not an omission. Web Packing → STR → "Fulfill Item" still loads
every line, ungated. Consequences are recorded under *Known gaps*.

## What changed

| File | Change |
|---|---|
| `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql` | Project `tl.custcol_dbti_vendor AS VendorId` and `BUILTIN.DF(...) AS VendorName`; `ORDER BY tl.id`; the four `inventorybalance` joins pre-aggregated with `SUM`; call-scoped `AND ({vendorId} = 0 OR tl.custcol_dbti_vendor = {vendorId})`. |
| `Integration.NS/NSScripts/NS_TO_x_Get_Vendor_Candidates.sql` | New: `SELECT DISTINCT tl.custcol_dbti_vendor AS Id` for one tranid, with the line read's record-type / category / status / `SHIPPING` filters and `IS NOT NULL`. |
| `Application.DataTransferObjects/Transactions/Commons/NS/TransferOrderLineDTO.cs` | `VendorId` (nullable) + `VendorName`. |
| `Application.DataTransferObjects/Transactions/Commons/NS/VendorLookupDTO.cs` | New: `{ Id, Name }` returned by the resolver; the candidate script fills only `Id`. |
| `Application.DataTransferObjects/Transactions/Commons/NS/Request/ResolveVendorRequestDTO.cs` | New: `OrderNumber` + `VendorBarcode` (the scanned name). |
| `Shared.Libraries/ViewModel/TransferOrder/TransferOrderLineVM.cs` | Same two fields, mapped by name. |
| `Shared.Libraries/ViewModel/TransferOrder/VendorLookupVM.cs` | New: mobile response shape for the resolver. |
| `Application.DataTransferObjects/Transactions/Commons/NS/Request/TransferOrderLineRequestDTO.cs` | Optional `VendorId`. Null/0 = every line. |
| `Application.UseCases/.../NS/TransferOrder/GetTransferOrderLineQry.cs` | Pages the read through `NetsuiteQueryWithMetadata` (1,000 rows/page, stable order, `totalResults`-terminated, 100-page cap, fail-loud); always sends `vendorId` (`id` or `"0"`); no in-memory filter. |
| `Application.UseCases/.../NS/TransferOrder/ResolveTransferOrderVendorQry.cs` | Document-scoped name matcher + handler. The handler sends `NS_TO_x_Get_Vendor_Candidates`, enriches via `GetVendorsByIdsAsync`, and maps infrastructure faults to `ApiResult.ServerError`. |
| `Application.UseCases/Repositories/Integration/Others/IVendorIntegration.cs` | New `GetVendorsByIdsAsync` for the document's vendor records. |
| `Integration.NS/Implementations/Others/VendorIntegration.cs` | Implementation: `id IN (...)` read of `entityid`, `companyname`, and `fullname`. |
| `Integration.NS/Implementations/Others/Caching/CachedVendorIntegration.cs` | Delegates the new read uncached — a six-hour cache could pin a renamed vendor. |
| `Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs` | New `ResolveVendor` action. Answers **400, never 404** — the mobile client turns 404 into "the endpoint does not exist". |
| `Mobile.MAUI/.../Packing/TransferOrder/TransferOrderItemView.razor(.cs)` | Vendor gate, searchable dropdown of vendors on the current order as a barcode fallback, name resolution call, inline "Resolving vendor…" indicator (outside `<Loading>`, which would blank the header), vendor badge shown before lines, scan routing, three-way Change Vendor (Save now / Discard / Cancel), clear-on-switch, and the `_resolvingVendor` busy guard that spans the follow-up item load. |
| `Integration.NS.Tests/.../Packing/StrPackingVendorFilterShapeTests.cs` | `tranid` travels as `@tranid`; the vendor travels as `{vendorId}`; an unscoped call sends `"0"`, a scoped call the id. |
| `Integration.NS.Tests/.../Packing/TransferOrderVendorResolverTests.cs` | Candidate-based matching: exact/case/whitespace/punctuation, record `entityid` / `companyname` / `fullname`, unknown and ambiguous names, blank scan, no candidates, ids without names, and a vendor beyond row 1,000. |
| `Integration.NS.Tests/.../Packing/TransferOrderLinePagingTests.cs` | New: 0/999/1,000/1,001/2,000 rows, call count, order, uniqueness, page failure, `totalResults` contradiction, page-cap overflow, cancellation. |

## What was deliberately not changed

1. **Web packing** — reverted to its original behaviour. No gate, no scan box, no
   vendor column, no `vendor` query parameter.
2. **The shared script's joins and parameters** — originally unchanged beyond
   projections. The 2026-10-22 hardening added the stable order, pre-aggregated the
   four `inventorybalance` joins, and added the call-scoped `{vendorId}` predicate;
   the `ivb` vendor-bin join still uses `itemvendor.preferredvendor`.
3. **Save path** — `PostItemFulfillment` and `SaveTOItemFulfillment` untouched.
   Mobile still posts only the lines the operator scanned; the web handler's
   zero-quantity filler behaviour is unchanged.
4. **Print output** — scriptlet 1671 is not in this repo.
5. **Receiving, trip ticket, returns, VRA** — untouched.

## Operational behaviour

| Situation | Result |
|---|---|
| Unknown name while gated | Warning naming the scan; gate stays open. |
| Scan while a vendor resolution is in flight | Swallowed (busy guard). The scanner's next scan is handled once resolution finishes. |
| Document barcode scanned while gated | "That is the document barcode — it is already open. Now scan the vendor barcode." |
| Vendor has no open lines on the order | Line lists empty, warning toast. Gate stays resolved so the operator can switch. |
| Vendor has only closed/fully-shipped lines | Same as above; those lines land in Done, not Good/Bad. |
| Lines with no vendor (legacy documents) | Excluded from every vendor scan. See *Known gaps*. |
| Refresh pressed while gated | No-op (nothing loaded yet). |
| Change Vendor, no unsaved scans | Gate reopens immediately. |
| Change Vendor, unsaved scans | Prompts Save now / Discard / Cancel. Save succeeds → switch; save fails → current vendor and scans retained; Discard → lists cleared; Cancel → unchanged. |
| Save | Unchanged command; mobile posts only the scanned lines. |

Only the `Packing/TransferOrder` handheld screen is affected. The STR receiving screen
(`TOxItemFulfillmentItemView`) and its item-fulfillment list are not gated.

## Verification

- `dotnet build` from the solution root — 0 errors (all projects).
- `dotnet test Integration.NS.Tests` — 69/69 packing tests pass; full suite
  521 passed, 1 failed:
  - `NetSuiteApiClientServiceFreshnessRevalidationTests.Newly_closed_scanned_line_is_reported(quantityShipped: 50, isClosed: "F")`
    — **pre-existing and unrelated.** `MergeFreshTOFulfillmentLineState`
    (`NetSuiteApiClientService.cs:1608`) only reports a scanned line when
    `IsLineClosed` is true, so the "fully shipped but not closed" case is never
    reported. Worth a separate fix.
- `StrPackingVendorFilterShapeTests` — 6/6: `tranid` as `@tranid`, scoped vendor as
  `{vendorId}`, unscoped `"0"`, and the scoped/unscoped row sets.
- `TransferOrderVendorResolverTests` — 13/13, including a vendor beyond row 1,000.
- `TransferOrderLinePagingTests` — page-size/total matrix, exact-multiple stop,
  page failure, short-page contradiction, page-cap overflow, cancellation.

Still to run against a live account / device (T16):

1. SuiteQL console: edited script with `{vendorId}` = `0` returns the tranid's
   `COUNT(*)` (1,657 on the sample); with a real id returns only that vendor's lines;
   `JoinedRows == DistinctLines`.
2. SuiteQL console: candidate script returns 95 ids for the sample tranid, no `NULL`
   row, clean `VendorLookupDTO` deserialization.
3. Postman: unscoped `/Items` returns 1,657; scoped `/Items` returns the vendor
   slice. Record both timings here.
4. Web `PostItemFulfillment` post succeeds with the grown payload (it now sees every
   line instead of ≤1,000 and posts a zero-quantity entry per non-completed line).
   If it cannot be exercised, treat the payload-size growth as an accepted risk.
5. Device: Change Vendor save-succeeds / save-fails / discard / cancel.

Not yet verified against a device or a live NetSuite account.

## Still required before deploy

1. **Device check on a second vendor.** The resolver now covers the vendor record's
   `entityid`, `companyname`, and `fullname` in addition to the line's display name,
   so any of them resolves. Confirm on a vendor whose printed label carries
   punctuation or a suffix (`CO., LTD`, `INC.`).
2. **Run the script in the SuiteQL console** for a real tranid and confirm `VendorId`
   / `VendorName` come back populated and that the row count is unchanged.
3. **Device smoke test** on a multi-vendor STR: scan the page, open it, scan vendor A,
   verify only A's items list, scan B, verify A's unsaved scans are gone and B's items
   appear.
4. **Double-fire scan check.** Scan the vendor barcode twice in quick succession
   (and again during a slow load). The busy guard swallows the second scan; the state
   must stay on the first-resolved vendor, with no cleared lists afterwards. This
   guard is UI-level and not covered by the automated tests.

## Known gaps

1. **Null-vendor lines are invisible.** Documents created before per-line vendors
   existed (or lines written by a path that does not set the column) match no vendor
   scan. They cannot be fulfilled from the handheld. Treat as a data/backfill item.
2. **The web packing flow is ungated.** Anyone using `Packing → STR → Fulfill Item`
   can still fulfill all vendors at once. Accepted for this change; if the rule must
   be absolute, either gate the web flow too or restrict that page.
3. **No server-side re-validation of vendor on save.** The gate is a query/UI control,
   and `PostTransferOrderIFCmd` accepts whatever line set it is given. Acceptable for
   an internal handheld; a crafted client could still post other lines.
4. **The scan is matched by name, which is fragile.** Renaming the vendor between
   printing and scanning breaks the match, and two vendors on one order whose names
   collapse to the same text are rejected as ambiguous. The durable fix is to have
   print scriptlet 1671 encode the vendor internal id instead; the resolver can then
   try an id first and fall back to the name, so existing printouts keep working.
5. **Multi-page vendor sections.** If a vendor's items overflow one page, the vendor
   barcode only appears in that section's footer. Confirm with the print owner whether
   it should repeat on every page of the section.
6. **Mobile list scanning is a side effect.** Barcode #1 is handled by the existing
   list search (text filter), not a dedicated "scan to open" route. It works, but
   there is no explicit not-found message for an STR outside the pending pool.

## Rollback

Revert the files above; the SQL script and the DTOs/VMs are additive, so a straight
file revert plus an app restart is enough. No migration, no NetSuite change, and the
save path was never modified — so an in-flight fulfillment posts exactly as it does
today.
