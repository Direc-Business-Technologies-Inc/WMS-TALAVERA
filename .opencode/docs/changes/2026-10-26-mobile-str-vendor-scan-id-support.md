# Mobile STR fulfillment — vendor scan accepts internal ID or name

Date: 2026-10-26
Area: `Application.UseCases` (STR packing vendor resolver), `Application.DataTransferObjects`, `Mobile.MAUI` (comments only)
Type: fix — handheld vendor gate accepted only one of the two payloads operators can produce
Builds on: `2026-10-21-mobile-str-vendor-gated-fulfillment.md`, `2026-10-22-str-packing-line-read-analysis.md`

## Context

The mobile Packing → Transfer Order screen is gated by a vendor barcode: lines stay
hidden until the operator scans the vendor printed under the STR's `Vendor:` field.
The gate resolves the scan through `POST /Packing/TransferOrder/ResolveVendor`, which
was written against one observed payload — the vendor **name**
(`ISABELA GIOS COMMERCIAL, INC.`), field-confirmed on 2026-10-21.

Requirement (2026-10-26): *change the vendor barcode scanning logic to accept the
vendor ID **or** the vendor name.*

Two facts made this a small, contained fix:

1. "Vendor name" was already broad. The resolver matches against the vendor record's
   `entityid`, `companyname`, and `fullname`, not just the line's display text. A scan
   of the vendor's **Entity ID** (e.g. `IGCI`) already resolved. The only missing
   payload was the **NetSuite internal ID**.
2. The resolver was already document-scoped: it builds a map keyed by the transfer
   order's own candidate vendor ids. The internal ID is that map's **key**, so the new
   match needed no new query, no new endpoint, and no change to the SQL scripts.

## What changed

| File | Change |
|---|---|
| `Application.UseCases/.../Packing/NS/TransferOrder/ResolveTransferOrderVendorQry.cs` | New internal-ID channel in `TransferOrderVendorResolver.Resolve`: a positive integer payload that is a candidate on this document resolves immediately, before any name matching. Messages and XML docs updated to "vendor ID or vendor name". |
| `Application.DataTransferObjects/.../Request/ResolveVendorRequestDTO.cs` | Contract comment now states the payload is an internal id or a printed name. |
| `Mobile.MAUI/.../Packing/TransferOrder/TransferOrderItemView.razor.cs` | `ResolveVendorScan` summary comment updated. No behaviour change — the page still posts whatever it scanned. |
| `Integration.NS.Tests/.../Packing/TransferOrderVendorResolverTests.cs` | Five new tests (see *Coverage*). Class summary updated. |
| `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md` | §2 retitled and a dated note added for the ID channel. |

No change to: the candidate SQL, the line SQL, the controller action, the mobile
routing/serialization, the print template (NetSuite scriptlet 1671), or the save path.

## Resolution rules

Order of evaluation inside `TransferOrderVendorResolver.Resolve`:

1. Blank scan → "Scan the vendor barcode printed under the Vendor field — the vendor ID
   or vendor name."
2. No vendor on any line → existing "has no vendor on its lines / fulfil from web
   Packing" error.
3. **Internal ID**: `int.TryParse(payload, NumberStyles.None, InvariantCulture)` and
   `id > 0` and the id is a candidate on this document → return that vendor. The
   display name is the vendor's first known name, falling back to the id string.
4. **Name, strict**: `entityid` / `companyname` / `fullname`, whitespace- and
   case-insensitive.
5. **Name, loose**: punctuation-insensitive retry (labels and records can differ by a
   comma or a period).
6. Zero matches → rejected with the scan, the order number, and up to five vendor names
   on the order. More than one (possible only after the loose retry) → rejected as
   ambiguous.

Deliberate details:

- **Document scope is preserved.** The id is only accepted if it is already in the
  candidate map, so scanning another order's vendor id cannot open this order's gate.
- **ID wins over a numeric name.** If one vendor's internal id is `222` and another
  vendor's name is literally `"222"`, the scan resolves to the vendor whose **id** is
  `222`. The rule is fixed and unit-tested rather than inferred at runtime.
- **Only digits are accepted as an id.** `NumberStyles.None` after `Trim()` means
  `+111` and `111,` are treated as names, not ids. Leading zeros (`00111`) parse to
  `111`.
- **`0` and negatives fall through** to name matching, so they produce the normal
  "not a vendor" rejection instead of a special case.
- **Missing names do not block an ID scan.** If the vendor records could not be read
  but the candidate ids are known, an ID still resolves; the name path now says "Scan
  the vendor ID instead."

## Operational behaviour

| Situation | Result |
|---|---|
| Scan the vendor's internal id (e.g. `111`) while gated | Resolves; lines load for that vendor; header shows the vendor name. |
| Scan the printed vendor name (current label) | Unchanged — resolves through the name channels. |
| Scan the vendor's Entity ID (e.g. `IGCI`) | Unchanged — `entityid` is one of the name channels. |
| Scan an id that belongs to a vendor on another order | Rejected with "not a vendor ID or vendor name on transfer order …" plus the candidates on this order. Gate stays open. |
| Scan a numeric payload that is both an id and another vendor's name | Resolves to the **id** owner. Deterministic; pinned by test. |
| Vendor records unreadable, id scanned | Resolves; display name falls back to the id string. |
| Vendor records unreadable, name scanned | "The vendor names for transfer order … could not be read. Scan the vendor ID instead." |
| Scan `0`, `-5`, or junk | Rejected as a name; gate stays open. |

## Coverage

`TransferOrderVendorResolverTests` — 19/19 pass (14 existing + 5 new):

- `Scanned_internal_id_resolves_to_that_vendor`
- `Scanned_internal_id_from_another_document_is_rejected`
- `Scanned_internal_id_wins_over_a_numeric_name`
- `Scanned_internal_id_resolves_even_when_vendor_names_are_missing`
- `Zero_is_treated_as_a_name_and_rejected`

All existing name-path tests still pass unchanged, which is the regression guard for
the new channel sitting in front of them.

## Verification

- `dotnet build` (via the test project's dependency graph) — 0 errors.
- `dotnet test Integration.NS.Tests --filter FullyQualifiedName~TransferOrderVendorResolverTests`
  — **19/19 passed**.
- `dotnet test Integration.NS.Tests` — **650 passed, 1 failed**:
  - `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`
    — **pre-existing and unrelated.** The shared `HttpClient` reports a 30 s timeout
    while `GetHttpTimeoutSeconds()` defaults to 100. Nothing in this change touches
    `NetSuiteApiClientService`.

Not verified on a device or against a live NetSuite account.

## Still required before deploy

1. **Device test, both payloads.** On one multi-vendor STR, scan a name label, Change
   Vendor, then scan an internal id label (printed or generated ad hoc). Both must load
   only that vendor's lines.
2. **Confirm the real payload.** Check whether the ship floor's label actually carries
   an id today. If it is still the name, this fix is insurance rather than a workflow
   change; if a new label prints ids, it is required.
3. **Cross-order rejection on device.** Scan an id from a different open STR and confirm
   the candidate-hint warning appears and the gate stays open.
4. **Double-fire.** Scan the id twice quickly; the `_resolvingVendor` guard must swallow
   the duplicate and stay on the first-resolved vendor.

## Known gaps / accepted trade-offs

1. **Numeric-name collision is resolved by rule, not by data.** If the business ever has
   a vendor whose name is digits and equals another vendor's internal id on the same
   order, the id owner wins. That is the documented, tested behaviour; the alternative
   is asking the operator, which the workflow does not support.
2. **Barcode prefixes are not stripped.** Payloads like `V-111` or `ID:111` are treated
   as names and rejected. No evidence any label produces them; adding prefix handling
   without a real sample would be speculative.
3. **The durable fix is still the print template.** Having scriptlet 1671 emit the
   internal id removes the rename fragility of name matching and the punctuation
   retry. This change makes such a switch a no-op on the WMS side, so it can happen
   later without a handheld release.
4. **Scan-wide fragility is unchanged.** A renamed vendor still breaks a *name* scan
   printed before the rename. Scanning the id does not.

## Rollback

Revert the five files above. The change is additive inside one pure resolver plus test
and comment edits — no API shape change, no SQL, no migration, no NetSuite change, and
the save path was untouched. A rebuild and app restart are enough.
