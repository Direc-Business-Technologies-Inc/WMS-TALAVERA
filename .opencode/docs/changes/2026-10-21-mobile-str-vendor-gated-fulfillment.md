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
3. **Vendor gate.** The item lists stay empty and a "Scan Vendor Barcode" card is
   shown. Every scan is treated as a vendor scan, regardless of the Good/Bad toggle.
4. Operator scans barcode #2 → the name is posted to
   `POST /Packing/TransferOrder/ResolveVendor`, which matches it against the
   vendors on **this order's** lines and answers `{ Id, Name }`.
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

1. Read the order's lines (the same read the fulfillment screen needs anyway).
2. Collapse the distinct `(VendorId, VendorName)` pairs, then read those vendor
   records for `entityid`, `companyname`, and `fullname`.
3. Match the scan against all of those names — whitespace/case-insensitive first,
   then a punctuation-insensitive retry (printed labels and record names can differ
   by a comma or a period).
4. Return the matched vendor's id and the name that matched.

The line projection alone is not enough: it carries `BUILTIN.DF`'s display text,
which is not guaranteed to be the name the print template rendered. The first field
attempt (2026-10-21) resolved to nothing until the vendor record's own name fields
were added to the candidate set.

Why scope the vendor read to the document's ids rather than a global name lookup:

- Only vendors already on the order are candidates, so a duplicate display name
  elsewhere in the vendor list cannot make the scan ambiguous.
- It uses the same record the print template read from, so one of `entityid` /
  `companyname` / `fullname` matches by construction.
- The extra read is one small `id IN (...)` query, not a scan of the vendor table.

A scan that matches nothing is rejected with the scanned text in the message. A scan
that matches more than one vendor (possible only after the punctuation-insensitive
retry) is rejected rather than guessed.

### 3. The filter lives in the query handler, not in the SQL script

`NS_TO_x_Packing_Get_Items.sql` is shared by three consumers: mobile line list, the
save path, and (historically) the web grid. Two constraints follow:

- **No `@vendorId` parameter in the script.** `SuiteQLText.SubstituteScriptParameters`
  only replaces parameters that are present in the dictionary; an unsubstituted
  `@vendorId` reaches NetSuite as invalid SQL and fails the request. Every caller
  would have to pass it, including the save path that must stay unfiltered.
- **The save path must see every line.** `ItemReceiptPackingHandler.PostItemFulfillment`
  rebuilds the payload from **all** non-completed source lines and posts zero-quantity
  entries for the ones that were not submitted. That is what keeps the other vendor's
  lines open. Filtering its input would change the payload the transform receives.

So the script gained **projections only** (`VendorId`, `VendorName`) and the filter is
applied in `MGetTransferOrderLineQryHandler` on the returned lines.

### 4. The save logic is untouched

`PostTransferOrderIFCmd` / `SaveTOItemFulfillment` are unchanged. "Fulfill only this
vendor's items" already follows from the line set on the handheld: unsubmitted lines
are posted as zero. No new command, flag, or guard.

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
| `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql` | Project `tl.custcol_dbti_vendor AS VendorId` and `BUILTIN.DF(...) AS VendorName`. No predicate, no parameter, no join change. |
| `Application.DataTransferObjects/Transactions/Commons/NS/TransferOrderLineDTO.cs` | `VendorId` (nullable) + `VendorName`. |
| `Application.DataTransferObjects/Transactions/Commons/NS/VendorLookupDTO.cs` | New: `{ Id, Name }` returned by the resolver. |
| `Application.DataTransferObjects/Transactions/Commons/NS/Request/ResolveVendorRequestDTO.cs` | New: `OrderNumber` + `VendorBarcode` (the scanned name). |
| `Shared.Libraries/ViewModel/TransferOrder/TransferOrderLineVM.cs` | Same two fields, mapped by name. |
| `Shared.Libraries/ViewModel/TransferOrder/VendorLookupVM.cs` | New: mobile response shape for the resolver. |
| `Application.DataTransferObjects/Transactions/Commons/NS/Request/TransferOrderLineRequestDTO.cs` | Optional `VendorId`. Null/0 = every line. |
| `Application.UseCases/.../NS/TransferOrder/GetTransferOrderLineQry.cs` | In-memory vendor filter when `VendorId is > 0`. Null-safe on the query result. |
| `Application.UseCases/.../NS/TransferOrder/ResolveTransferOrderVendorQry.cs` | New: document-scoped name matcher (`TransferOrderVendorResolver`) + query handler. |
| `Application.UseCases/Repositories/Integration/Others/IVendorIntegration.cs` | New `GetVendorsByIdsAsync` for the document's vendor records. |
| `Integration.NS/Implementations/Others/VendorIntegration.cs` | Implementation: `id IN (...)` read of `entityid`, `companyname`, and `fullname`. |
| `Integration.NS/Implementations/Others/Caching/CachedVendorIntegration.cs` | Delegates the new read uncached — a six-hour cache could pin a renamed vendor. |
| `Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs` | New `ResolveVendor` action. Answers **400, never 404** — the mobile client turns 404 into "the endpoint does not exist". |
| `Mobile.MAUI/.../Packing/TransferOrder/TransferOrderItemView.razor(.cs)` | Vendor gate, name resolution call, scan routing, vendor badge, Change Vendor, clear-on-switch, empty/refresh guards, failure toast on the item load, and the `_resolvingVendor` busy guard that swallows scans arriving while a vendor resolution is in flight (double-fired scanners and slow networks otherwise interleave two resolutions and can leave the operator scanning against the wrong vendor). |
| `Integration.NS.Tests/.../Packing/StrPackingVendorFilterShapeTests.cs` | New: vendor filter returns only that vendor; null vendor returns all; null-vendor lines are excluded when filtering; the vendor never enters script parameters. |
| `Integration.NS.Tests/.../Packing/TransferOrderVendorResolverTests.cs` | New: exact/case/whitespace/punctuation matching, unknown and ambiguous names, blank scan, null-vendor lines. |

## What was deliberately not changed

1. **Web packing** — reverted to its original behaviour. No gate, no scan box, no
   vendor column, no `vendor` query parameter.
2. **The shared script's `WHERE`, joins, and parameters** — only projections were
   added. The `ivb` vendor-bin join still uses `itemvendor.preferredvendor`.
3. **Save path** — `PostItemFulfillment` and `SaveTOItemFulfillment` untouched.
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
| Change Vendor | Clears unsaved scans, reopens the gate. |
| Save | Unchanged command; other vendors' lines are posted as zero and stay open. |

Only the `Packing/TransferOrder` handheld screen is affected. The STR receiving screen
(`TOxItemFulfillmentItemView`) and its item-fulfillment list are not gated.

## Verification

- `dotnet build Web.BlazorServer` — 0 errors (web reverted, still green).
- `dotnet build Api.CoreWebAPI` — 0 errors.
- `dotnet build Mobile.MAUI` — 0 errors (re-run after adding the `_resolvingVendor`
  busy guard; the ~1470 warnings are pre-existing project-wide).
- `StrPackingVendorFilterShapeTests` — 4/4 pass.
- `TransferOrderVendorResolverTests` — 12/12 pass (exact, case/whitespace, punctuation
  fallback, vendor-record `entityid` / `companyname` / `fullname` fallback, unknown
  name with candidate hint, ambiguous name, blank scan, null-vendor lines, no-vendor
  order, ids without any name).
- Full `Integration.NS.Tests` — 503 passed, 2 failed, both unrelated to this change:
  - `NetSuiteApiClientServiceFreshnessRevalidationTests.Newly_closed_scanned_line_is_reported(quantityShipped: 50, isClosed: "F")`
    — **pre-existing** and consistent. `MergeFreshTOFulfillmentLineState`
    (`NetSuiteApiClientService.cs:1608`) only reports a scanned line when
    `IsLineClosed` is true, so the "fully shipped but not closed" case is never
    reported. Not touched by this change; worth a separate fix.
  - `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`
    — the known static-`HttpClient` flake; it passes in isolation.

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
