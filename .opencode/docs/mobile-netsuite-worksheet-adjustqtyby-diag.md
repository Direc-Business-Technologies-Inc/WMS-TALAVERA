# Mobile Inventory Worksheet — Scan & Save Diagnostics

**Status:** Diagnostics shipped (API-only) + scan/save guards shipped (APK). Temporary `[diag]` marker — remove after sign-off.
**Owner paths:** `Mobile.MAUI/.../CreateInventoryWorksheetView.razor.cs`, `Integration.NS/Services/NetSuiteApiClientService.cs`
**Related:** [mobile-netsuite-4xx-catalog.md](mobile-netsuite-4xx-catalog.md) (section 5), [WMS_PROCESS_FLOW.md](WMS_PROCESS_FLOW.md) (5.4.C)

> Filename retained for link stability. This document now covers **two distinct
> failures** on the same screen: the original `adjustqtyby` rejection (§3) and the
> silent scan-registration failure that surfaces as `EMPTY_LINES` (§2).

---

## 1. The two failures are not the same bug

| | Failure A — nothing registers | Failure B — value too long |
|---|---|---|
| NetSuite code | `EMPTY_LINES` | `INVALID_FLD_VALUE` (`adjustqtyby`) |
| Symptom | operator scans, nothing appears, Save fails | operator scans fine, Save fails |
| Payload | `lines: []` | `lines` populated, one number > 15 chars |
| Owner | mobile scan path | payload builder **or** Restlet 1878 |
| Status | **fixed in this change** | **parked** (needs the offending value) |

Do not let field reports merge these. The `[diag …]` marker distinguishes them in
one screenshot: `lines=0` is Failure A, `lines>0` is Failure B.

---

## 2. Failure A — scans silently discarded (`EMPTY_LINES`)

### 2.1 Observed

```
Action failed: Error while posting Inventory Worksheet. NetSuite 400 Bad Request
{"error":{"code":"EMPTY_LINES","message":"The lines array must not be empty."...
[diag lines=0 in=0 zeroQty=0 build=1.0.0+a7cec134…]
```

### 2.2 Why an empty payload reached NetSuite

The app had three independent "quiet" holes. Any one of them produced a save that
looked like a network error:

1. **Display filters hide zero-quantity lines.**
   `GoodScannedItems`, `BadScannedItems` and `AllScannedItems` all filter
   `!= 0` (`CreateInventoryWorksheetView.razor.cs:68-80`). A line recorded with
   quantity `0` exists in `ICItems` but is **invisible** — which is exactly the
   operator's report, "it does not register and is not displayed."
2. **Save is enabled by line count, not by quantity.**
   `SaveBtnDisabled => ICItems.Count == 0` (`:58`) is a `Count` check. Lines with
   zero quantity keep the button live.
3. **The API filter then empties the payload.**
   `PostInventoryWorksheetPayloadDTO.cs:57` keeps only
   `GoodScannedQuantity > 0 || BadScannedQuantity > 0`, so zero-quantity lines are
   dropped and the builder emits `lines: []`.

### 2.3 The silent paths that could produce zero or missing lines

| # | Path | Before | Now |
|---|---|---|---|
| A1 | `catch (NotAuthorizedException) { }` in `HandleScan` (`:437-440`) | expired session made **every** scan a silent no-op | toasts *"Session expired. Please sign in again."* |
| A2 | `if (MoveOn || NegateQuantity) return;` in `ProcessScannedItem` (`:467-471`) | pressing Move/Remove swallowed every new scan with no feedback | toasts *"Move/Remove mode is on…"* |
| A3 | `quantity = barcode.UoMRate` (`:487`) | a `0` rate added the line with quantity `0` — invisible, and later filtered out | refuses with *"<item> has no UoM conversion rate. Scan not recorded."* |
| A4 | `SaveScan()` (`:575-585`) | posted whatever was in `ICItems`, including all-zero lists | refuses with *"No scanned quantities to save."* |

A3 is a guard, not a confirmed root cause: a SuiteQL audit of
`customrecord_barcode_per_uom` joined to `unitstypeuom` returned **no rows** with a
null, zero or fractional `conversionrate`, so the NetSuite data is clean. The guard
exists because a `0` rate would otherwise be silently accepted.

### 2.4 The invariant this restores

> A scan either changes a quantity that the operator can see, or it produces a
> message naming why it did not. The app never posts a worksheet with no positive
> quantity.

---

## 3. Failure B — `adjustqtyby` exceeds 15 characters (parked)

```
{"error":{"code":"INVALID_FLD_VALUE","message":"...Invalid Field Value for the field
 adjustqtyby contained more than the maximum number (15) of characters allowed."}}
```

The WMS payload has no `adjustqtyby` field; Restlet `script=1878` writes it and is
**not in this repository**. The payload builder emits `newqty` as an unrounded
`decimal` sum (`PostInventoryWorksheetPayloadDTO.cs:97`), and `decimal` preserves
scale through `System.Text.Json`, so a repeating fraction would arrive as e.g.
`3.3333333333333333333333333333` (30 chars).

Resolution is gated on one value. Read the marker:

- `maxLen > 15` → **ours**. Round to 5 dp at the builder, matching the nine other
  mobile save paths (`FormatHelper.RoundOfNearestHundredThousands`).
- `maxLen <= 15` → **Restlet 1878**. Escalate to the NetSuite team with the
  `qty`/`len` values and the payload from the server log.

---

## 4. The `[diag …]` marker

Appended to the error message the mobile toast already renders, so the operator's
screenshot is the diagnostic — no device logs and no server-log access required.

```
[diag lines={n} scanned={n} item={invtid} qty={value} len={n} maxLen={n} build={stamp}]
[diag lines=0 in={n} zeroQty={n} build={stamp}]
```

| Field | Meaning |
|---|---|
| `lines` | worksheet lines **posted** to NetSuite |
| `in` | worksheet lines the **phone sent** to the API |
| `zeroQty` | of those, how many had no positive quantity |
| `scanned` | posted lines with a positive quantity |
| `item` / `qty` / `len` | the longest-numbered line and its serialized length |
| `maxLen` | longest serialized number across `newqty` and every `inventoryDetail[].quantity` |
| `build` | API assembly informational version |

### Reading the empty cases

| Marker | Meaning | Next action |
|---|---|---|
| `in=0` | the phone sent an empty list | scan registration failed — read the toast the operator sees *while scanning* |
| `in=3 zeroQty=3` | phone sent 3 lines, all quantity 0 | quantities were lost between resolution and save (A3) |
| `in=3 zeroQty=0 lines=0` | contradictory — report as a defect | capture the full payload from the server log |

Length is measured with `JsonSerializer.Serialize(value).Length`, not `ToString()`,
because serialized scale is what actually reaches the wire.

---

## 5. Changes applied

| File | Change |
|---|---|
| `CreateInventoryWorksheetView.razor.cs:433` | `await ProcessScannedItem(...)` (method is now `async Task`) |
| `CreateInventoryWorksheetView.razor.cs:437-440` | `NotAuthorizedException` now toasts instead of being swallowed |
| `CreateInventoryWorksheetView.razor.cs:451-459` | new guard: reject `UoMRate <= 0` with a named message |
| `CreateInventoryWorksheetView.razor.cs:467-471` | Move/Remove mode now toasts instead of returning silently |
| `CreateInventoryWorksheetView.razor.cs:581-585` | new save guard: refuse an all-zero worksheet |
| `NetSuiteApiClientService.cs:2056-2060` | empty-payload branch reports `in=` and `zeroQty=` |

Build verification: `Integration.NS` and `Mobile.MAUI (net9.0-android)` both compile
with 0 errors.

---

## 6. Testing this change

Deploy **both** the API and a new APK — the mobile guards are the actual fix; the API
change only improves the diagnostic.

| Step | Expected |
|---|---|
| Scan one valid barcode | line appears with quantity ≥ 1 |
| Scan with an expired session | toast *"Session expired. Please sign in again."* |
| Tap Move, then scan a new item | toast *"Move/Remove mode is on…"*, nothing added |
| Save with no scans at all | toast *"No scanned items to save."*, no NetSuite call |
| Save with lines recorded at quantity 0 | toast *"No scanned quantities to save."*, no NetSuite call |
| Save a normal worksheet | succeeds; a failure still carries the `[diag …]` suffix |

A save that reaches NetSuite and fails now means the payload was **not** empty —
that alone separates Failure A from Failure B.

---

## 7. What this does not prove

The guards make the app fail loudly; they do not prove the underlying reason scans
were discarded on the client device. That reason is identified by the **toast the
operator sees while scanning**, and by the `in=` field on the next failed save.

Also remember two facts that could not be established remotely:

- **Installed APK version is unknown.** `SaveBtnDisabled` (`:58`) and the
  `ICItems.Count == 0` guard imply an empty list could not have been saved — which
  points at zero-quantity lines (A3). If the device runs an older APK without those
  guards, "empty list" becomes the likelier explanation. Confirm the build on the
  device before interpreting the next report.
- **Barcodes absent from `customrecord_barcode_per_uom` are invisible to the audit
  query.** A barcode that is not in that table resolves as `NotFound` and is
  correctly refused; confirming one requires the physical barcode value.

---

## 8. Removal checklist (after sign-off)

1. Delete `BuildWorksheetDiag` and restore the plain throw in `PostInventoryWorksheet`.
2. Remove the `[diag]` entry from the 4xx catalog's open items.

Keep the §2 guards permanently — they are behaviour fixes, not diagnostics.
