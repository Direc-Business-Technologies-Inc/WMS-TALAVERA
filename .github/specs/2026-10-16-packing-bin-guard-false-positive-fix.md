# Fix: Packing bin guard blocks non-bin-tracked locations (false positive)

Date: 2026-10-16
Status: Fix in source, build clean, 332/332 tests green — **awaiting client verification on the original failing order**
Mode: Bugfix / Follow-up to [2026-10-15 IF replace-param fix](2026-10-15-if-replace-param-invalid-parameter-fix.md)
Scope: Packing module — Item Fulfillment pre-flight guard (STR, Returns packing, RTS / Vendor Return Authorization)
Related: [2026-10-15 IF replace-param fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) — this document closes out its follow-up #6 and corrects its Status line
Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs`
- `Integration.NS.Tests/Services/NetSuiteApiClientServiceBinRequirementTests.cs` (new)

## Summary

The pre-save bin guard shipped on 2026-10-15 (prior spec, "Case 1a", follow-up #6) rejects **every** packing save whose scanned lines resolve no bin, regardless of whether the location uses bins at all. It was verified in the field on 2026-10-16 and **refuted**:

```
Fulfill Packing Item failed: Cannot post GOOD TO Item Fulfillment:
location 'SELLING AREA - CAUAYAN' is bin-tracked but no bin is resolved for:
item-list line 1 (WMS line 2) 00018051 - TALAVERA FRESH EGG LARGE/MED/JB/XL 1'S.
Assign a bin to these items before saving.
```

The location named in the error is **not** bin-tracked. NetSuite confirmed, and the operator verified directly:

```sql
SELECT id, name, usebins FROM location WHERE name LIKE '%CAUAYAN%';
-- 108 | SELLING AREA - CAUAYAN JR | F
--  16 | SELLING AREA - CAUAYAN    | F
```

So the order — item `00018051`, 150 Good / 0 Bad, From Subsidiary `TGC : THM 000 HO : THM 001 MAIN`, From Warehouse `SELLING AREA - CAUAYAN`, To Warehouse `SELLING AREA - CAUAYAN JR` — is a **valid** packing transaction that the guard blocks. The error message asserts a fact (`is bin-tracked`) that the code never checked; it was derived text.

The guard was not wrong in intent. It was **incomplete**: it validated the bin-resolution consequence without consulting the bin-tracked cause.

## Root Cause

The guard predicate, repeated verbatim at five call sites, tested only the consequence:

```csharp
if (line.ScannedQuantity > 0 && line.NetsuiteMaterialVendorAssignedBin == 0 && line.NetsuiteMaterialPrefferedBinId == 0)
```

`isUsedBin` — the flag that answers *"does this location use bins?"* — was computed 70 lines earlier and used only to pick the payload shape and the `?replace=` URL. It never reached the guard.

**Why `0` is legitimate at a non-bin location.** Both bin ids arrive through `LEFT JOIN`s in the packing scripts (`NS_TO_x_Packing_Get_Items.sql:57-83` and siblings). At a location with `usebins = 'F'` there is no bin row and no vendor bin assignment to join, so both ids are `0` **by construction**. For those lines the guard's premise — "a bin should have resolved and did not" — is false.

The result is a two-sided defect:

| Location type | Guard behaviour | Correct behaviour |
|---|---|---|
| Bin-tracked, bin resolved | passes | passes |
| Bin-tracked, no bin | blocked with an accurate message | blocked with an accurate message |
| **Not bin-tracked** | **blocked with a message claiming it *is* bin-tracked** | passes, posts the legacy no-detail shape |

Rows 1–2 were the intended design. Row 3 was unhandled and blocks all packing at non-bin locations.

### The flag pipeline is sound (verified, not assumed)

Two claims from the NetSuite side were checked against source before changing anything, because the whole fix rests on `LocationUsedBin` being trustworthy:

1. **"it joins to the location table, there's a column `usebins` — that's what alters the payload"** — **confirmed.** `NS_TO_x_Packing_Get_Items.sql:54` is `JOIN location loc ON tl.location = loc.id` (an **inner** join, so the flag is never null), and `:17` projects `loc.usebins AS LocationUsedBin`. The same projection exists in all six packing/receiving line queries. `ItemReceiptPackingHandler.cs:173` parses it via `IsNetSuiteTrue`, which accepts only `"T"`/`"true"` — so `usebins = 'F'` correctly yields `IsLocationUsedBin = false`. The payload builder's bin branch (`TransferOrderIFPayloadDTO.cs:82`) keys off exactly this flag.

2. **"custom record Vendor Bin Assignment, basis is location + subsidiary + vendor; if it matches, take the bin from that custom record"** — **confirmed, plus an item leg.** `NS_TO_x_Packing_Get_Items.sql:71-83`:

   ```sql
   FROM itemvendor iv
   JOIN customrecord_dbti_vendor_bin_assignment ba ON iv.vendor = ba.custrecord_dbti_vba_vendor
   WHERE iv.preferredvendor = 'T'
   GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location
   ) ivb ON ivb.item = i.id
       AND ivb.subsidiary = t.subsidiary
       AND ivb.custrecord_dbti_vba_location = tl.location
   ```

   Basis is item + subsidiary + vendor + location. Precedence is already **vendor-assigned bin → preferred bin → null** (`TransferOrderIFPayloadDTO.cs:82-93`, identical in the Returns and VRA builders). No change required; this is also why the new error message names both data sources as the remedy.

## The Fix

### 1. One testable predicate, gated on the location flag

`NetSuiteApiClientService.cs:1107-1119`:

```csharp
internal static bool RequiresBin(bool isUsedBin, decimal scannedQuantity, int vendorAssignedBin, int preferredBin) =>
    isUsedBin && scannedQuantity > 0 && vendorAssignedBin == 0 && preferredBin == 0;
```

Made `internal static` deliberately, mirroring `DeduplicateFulfillmentLines`, so the rule is reachable from unit tests instead of only through a live NetSuite post.

**Gated on the header-level `isUsedBin`, never per-line.** This is the one non-obvious choice in the fix. The payload builder and the `?replace=` URL are both selected from the *header* flag; a per-line gate would pass a stale non-bin line through validation, then the transform would still demand `inventoryDetail` for the whole save and NetSuite would reject it with the opaque `Please configure the inventory detail in line N`. Matching the guard's gate to the payload's gate is what keeps the two in step.

### 2. All five guard sites now call it

`:1151` (TO bad) · `:1201` (TO good) · `:1272` (Returns) · `:1349` (VRA bad) · `:1398` (VRA good) — a one-line substitution each. The surrounding comments already said *"at bin-tracked locations"*; the code now matches them.

### 3. Mixed-flag fail-fast extended to Returns and VRA (required, not optional)

`isUsedBin` is an **OR across lines** (`Data.Any(...)`). Once the guard is gated, a save whose lines disagree about `usebins` would take `isUsedBin = true`, resolve bins for the non-bin lines, find none, and fail with the old misleading message — or, for the bin-tracked lines among them, post correctly while the others silently lost their detail.

`SaveTOItemFulfillment` already had this check (`:1137`). It is now present in all three flows — `:1137` (TO), `:1260` (Returns), `:1331` (VRA) — failing loud with a sample of the offending rows and the reload instruction. This closes the parity gap that the prior spec's "Behaviour Matrix" silently depended on.

### 4. Operator-actionable messages

All five messages reworded from *"Assign a bin to these items before saving."* to *"Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items."*

Two reasons: there is **no bin control anywhere in the mobile packing UI** (no packing `.razor` in `Mobile.MAUI` references a bin; the client only echoes `LocationUsedBin` / `NetsuiteMaterialPrefferedBinId` / `NetsuiteMaterialVendorAssignedBin` from the server payload), so "assign a bin" was unactionable. And with fix #1 these messages now only fire when the location genuinely uses bins, so the claim is finally true.

## Tests

**New:** `NetSuiteApiClientServiceBinRequirementTests` — pins the location-flag repro (`usebins = 'F'` with both bin ids `0` requires nothing), the genuine block (`usebins = 'T'`, no bin resolved), both resolution paths (vendor bin, preferred bin, both), zero-qty filler lines, and that the location flag alone decides.

**Existing and still green:** `PackingIFPayloadDTOTests` (7 cases, including `Returns_non_bin_location_sends_no_inventory_detail`), `NetSuiteApiClientServiceFulfillmentDedupeTests` (6 cases).

Build: `dotnet build Integration.NS` → **0 errors** (15 pre-existing warnings in untouched files).
Tests: full `Integration.NS.Tests` → **332/332 passed**.

> Note on flakiness: `GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks` and `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default` each failed once across repeated full-suite runs and **pass in isolation** — the first is a 30s `MemoryCache` convergence poll whose own comment at `:233-236` acknowledges parallel runs can outlast the deadline. Pre-existing, unrelated, and already noted in the 2026-10-15 spec.

## Behaviour Matrix (current code)

| Location | Scanned line, bin resolved | Scanned line, no bin | Filler line (qty 0) | Payload / URL | Status |
|---|---|---|---|---|---|
| Not bin-tracked | n/a (no bins exist) | **passes — posts the legacy shape** | `{orderLine, itemreceive:false}` | no `replace`, no `inventoryDetail` | **This fix. Awaiting client repro** |
| Bin-tracked | `+ inventoryDetail[status, qty, bin]` | blocked, accurate message + remedy | `+ inventoryDetail.inventoryAssignment.items: []` | `?replace=…inventoryAssignment` | Unchanged by this fix |
| Mixed flags in one save | — | blocked on all three flows with a reload instruction | — | — | **New for Returns and VRA** |

Not affected: PO Item Receipt, TO receiving (restlet), Returns Item Receipt, Inventory Counting. The receiving-side payload builders still inline their own `inventoryStatus` mapping rather than routing through `InventoryStatusMap` — a pre-existing drift noted in the 2026-10-15 spec, out of scope here.

## Verification Plan

Must be confirmed on the client; no NetSuite sandbox was available in this session.

1. **The failing order — non-bin STR (the repro).** SELLING AREA - CAUAYAN → CAUAYAN JR, item `00018051`, 150 Good / 0 Bad. Must save. This is the case the fix targets.
2. **Bin-tracked STR, no bin resolved** — must still be blocked, with the corrected message. The prior spec's guard was correct here and must not be weakened.
3. **Bin-tracked STR with resolved bins** — unchanged; bins and statuses still post.
4. **Returns packing and RTS/VRA** at both a bin-tracked and a non-bin location.
5. **Mixed-flag save** — confirm the new fail-fast fires and names the offending rows rather than posting a partial detail.
6. **PO Item Receipt sanity check** — untouched path.

### Pre-test housekeeping

Unchanged from the prior spec and still owed: BAD posts before GOOD with no compensation, so reconcile any transfer order left partially fulfilled by earlier failed attempts in NetSuite before retesting. Prefer a scratch order over a live shipment.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Gate on header-level `isUsedBin`** (chosen) | *Per-line `line.IsLocationUsedBin`*: would validate inconsistently with the header-driven payload shape and let a stale line post `binNumber: null` against a `replace=` transform — re-creating the opaque `line N` failure. |
| **Keep the guard, narrow it** (chosen) | *Delete the guard*: the bin-tracked no-bin case is a real NetSuite rejection, and the prior spec proved the message saves a field-debugging cycle. |
| **Add mixed-flag fail-fast to Returns and VRA** (chosen) | *Leave as-is*: once the guard is gated, a mixed save is the new failure mode, and `Any()` would otherwise silently downgrade every non-bin line. |
| **`internal static` predicate + unit tests** (chosen) | *Test through the service with a mocked HTTP client*: much heavier, and the rule is a pure predicate. |
| **Reword to name Vendor Bin Assignment** (chosen) | *Keep "assign a bin"*: there is no bin control in the mobile UI to act on. |

## Residual Risk

1. **Low–medium — the reverse case is now silent.** `IsNetSuiteTrue` (`ItemReceiptPackingHandler.cs:173` and the Returns/VRA handlers) accepts only `"T"`/`"true"`, so a genuinely bin-tracked location whose flag arrived as `"Y"`, `"1"`, or an unprojected null resolves to `false` → no guard, no `inventoryDetail`, no error. This pre-existed, but the guard used to be a partial backstop; it no longer is. **Recommended follow-up:** log a warning when a line carries bin ids while `IsLocationUsedBin` is false — it would have caught the mirror-image bug immediately.
2. **Medium — new rejections in Returns and VRA.** Mixed-flag saves that previously posted (incorrectly, without inventory detail on the non-bin lines) will now be blocked. Audit those queues before release.
3. **Medium — other non-bin locations.** Every location with `usebins = 'F'` has been unable to pack since the guard shipped on 2026-10-15. Both CAUAYAN locations were confirmed `F`; the blast radius beyond them was not enumerated.
4. **Medium — sequence integrity.** BAD-then-GOOD with no compensation; retries fail confusingly. Carried over unchanged.
5. **Low–medium — lot/serial-tracked items.** `InventoryAssignmentItem` has no lot/serial fields, so those items cannot satisfy inventory detail at a bin-tracked location regardless of bin data. Unchanged by this fix.
6. **Low — `ItemFulfillmentUrlNotUsedBin`** remains a dead field whose name is the semantic opposite of its value (it is byte-identical to `ItemFulfillmentUrl`). Cleanup only.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Gate the bin guard on `isUsedBin` via a testable `RequiresBin` predicate (5 call sites) | **Done** |
| 2 | Extend the `mixedBinFlags` fail-fast to Returns and VRA | **Done** |
| 3 | Reword the 5 guard messages to be operator-actionable | **Done** |
| 4 | Regression tests for the location-flag rule | **Done — `NetSuiteApiClientServiceBinRequirementTests`** |
| 5 | Client verification of the original failing order (SELLING AREA - CAUAYAN) | **Open** |
| 6 | Warn when bin ids are present but `IsLocationUsedBin` is false (reverse-case hardening) | Open — recommended |
| 7 | Enumerate all `usebins = 'F'` locations that have been blocked since 2026-10-15 | Open — data, not code |
| 8 | Reconcile transfer orders left partially fulfilled by the blocked saves | Pending |
| 9 | Remove the temporary `[diag …]` markers once all five flows sign off | Open — carried from 2026-10-15 |
| 10 | Remove dead field `ItemFulfillmentUrlNotUsedBin` | Open — cleanup |
| 11 | `graphify update .` (blocked in this session by sandbox bash rules) | Outstanding |

## Validation Limits

- **No live NetSuite verification.** All conclusions rest on source reading plus the operator's `usebins = 'F'` SuiteQL result. The behavioural claims in the Behaviour Matrix are derived, not observed.
- **The screenshot is the only field evidence**, and it names the same location the operator later queried; the two are consistent but were captured independently.
- **The blast radius of the bug is not measured.** It is inferred that every non-bin-tracked packing location has been blocked since the guard shipped, because the predicate is unconditional and both bin ids are `0` by construction there. The count and the affected order list are unknown.
- **The mixed-flag case was not exercised in the field.** The Returns and VRA additions mirror the existing, field-proven TO check, but Returns and VRA have never had that guard, so its behaviour on real mixed input is untested.
- The prior spec's Status line already recorded *"bin guard deployed but refuted"*; this document supplies the evidence and the remedy for that refutation. Its follow-up #6 should now be read as superseded by items 1–4 above.