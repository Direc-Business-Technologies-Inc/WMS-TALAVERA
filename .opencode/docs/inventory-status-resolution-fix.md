# Inventory Status Resolution — Packing Fulfillment Fix

> Status: implemented.
> Date: 2026-10-15.
> Scope: NetSuite inventory status resolution on packing (item fulfillment) payloads.
> Related: `mobile-netsuite-4xx-catalog.md` (sections 1a–1d remain open).

---

## 1. The reported error

```text
Fulfill Returns Item failed: Error while posting Returns Item Fulfillment.
NetSuite 400 Bad Request: Bad Request(400): [USER_ERROR] Error while accessing a resource.
You have entered an Invalid Field Value 3 for the following field: InventoryStatus
```

## 2. Account ground truth

Verified in NetSuite on 2026-10-15:

| Internal ID | Name |
|---|---|
| 1 | Good |
| 3 | Bad |
| 6 | Missing |

Query used:

```sql
SELECT id, name FROM inventorystatus WHERE isinactive = 'F'
```

**These IDs are account configuration, not NetSuite constants.** Any new account or
subsidiary must be re-verified before this code is pointed at it.

## 3. Root cause

`ReturnsIFPayloadDTO` resolved the status from the **order-level** transfer category
instead of the **per-line** condition:

```csharp
// before
Id = line.TransferCategory == 3 ? "1" : "3"
```

`TransferCategory` comes from `custbody_dbti_transfer_category`, a **header** field
(`StockTransferRequestIntegration.cs:483`), where `3 = Return - Good Items` and
`4 = Return - Bad Items`.

Two distinct defects fell out of that one line.

### 3a. Silent corruption — operator-marked damage booked as Good

The web packing grid exposes a bad-quantity column
(`ReturnsItemReceiptForm.razor:90-104`), and the web handler computes the correct
per-line flag:

```csharp
// ReturnsItemReceiptPackingHandler.cs:152
IsBad = submittedLine?.QuantityBad > 0 && submittedLine.QuantityGood == 0,
```

…which the payload builder discarded. An operator entering a bad quantity on a
Returns-Good order had that damage posted as status `1` (Good). **NetSuite accepts this
without error**, so there was no toast, no log and no 400 — just overstated stock,
surviving until a later reconciliation.

### 3b. Fail-open default on unmapped categories

`== 3 ? "1" : "3"` mapped `0`, `-1` and any unmapped value to **Bad**. An unmapped
category should refuse the save, not choose a status.

## 4. Why the paths had drifted

The **receiving** path already had the correct per-line mapping:

```csharp
// ReturnsIRPayloadDTO.cs:63  (receiving - was already correct)
Id = line.IsMissing ? "6" : line.IsBad ? "3" : "1",

// TransferOrderIRRestletPayloadDTO.cs:54
InventoryStatus = line.IsMissing ? 6 : line.IsBad ? 3 : 1,
```

The **packing** path was written separately and diverged — two-state instead of
three, order-level instead of per-line. Nothing enforced agreement, so the two halves
of the same domain concept drifted with no failing test.

A third instance of the same hardcoded assumption sits in the UI:

```csharp
// InventoryDetailsDialog.razor.cs:156-161
var cbtc = res.Item1.FirstOrDefault(x => x.Id == 3);
if (ChangeBadToConfiscated && cbtc != null)
{
    cbtc.Name = "CONFISCATED";
}
```

This overrides NetSuite's real status name at render time, and the `cbtc != null` guard
means a missing status `3` silently drops out of the dropdown instead of erroring —
the same fail-open pattern as 3b.

## 5. The fix

### 5.1 Single source of truth

New file: `Application.DataTransferObjects/Transactions/Commons/NS/Payload/InventoryStatusMap.cs`

```csharp
public static string Resolve(bool isBad, bool isMissing, int? transferCategory = null)
{
    if (isMissing) { return Missing; }
    if (isBad) { return Bad; }
    return transferCategory == ReturnCategoryBad ? Bad : Good;
}
```

Precedence is **per-line condition first, order-level category second**. That ordering
is the fix: an explicit operator decision can never be overridden by a header field.

### 5.2 Builders wired to the resolver

| File | Change |
|---|---|
| `ReturnsIFPayloadDTO.cs:76` | `TransferCategory == 3 ? "1" : "3"` → `InventoryStatusMap.Resolve(line.IsBad, line.IsMissing, line.TransferCategory)` |
| `TransferOrderIFPayloadDTO.cs:76` | `IsBad ? "3" : "1"` → same resolver |
| `VendorReturnAuthorizationIFPayloadDTO.cs:76` | `IsBad ? "3" : "1"` → `Resolve(line.IsBad, isMissing: false)` (VRA has no transfer category) |

Status `6` (Missing) is now reachable on the packing path, matching receiving.

### 5.3 Fail-closed validation

`PostReturnsIFCmd.cs` now refuses two conditions **before** the HTTP call, following the
trip-ticket exemption precedent already in that file (`:29-41`):

- **All-zero scan** — "Please enter at least one quantity to fulfill. Nothing was saved."
  Previously posted a zero-quantity body and returned an opaque NetSuite 400.
- **Unrecognised transfer category** — names the order, line and offending value.

Placing these in the command means **web and mobile share one set of rules**, since both
dispatch `PostReturnsIFCmd` (`ReturnsController.cs:46` and
`ReturnsItemReceiptPackingHandler.cs:64` respectively).

The dead commented-out guard in `ReturnsItemReceiptPackingHandler.cs:59-62` was removed;
it is superseded by the command-level check.

## 6. Behaviour change

| Line condition | Order category | Before | After |
|---|---|---|---|
| good | Good (3) | `1` | `1` |
| **bad** | **Good (3)** | **`1` — wrong** | **`3` — fixed** |
| good | Bad (4) | `3` | `3` |
| bad | Bad (4) | `3` | `3` |
| missing | any | `1` | `6` |
| unmapped category | `0`/other | `3` — guessed | **save refused** |

Mobile behaviour is unchanged for the common path: `Packing/Returns/ReturnsItemView.razor.cs:121`
hardcodes `IsBad = false`, so mobile still falls back to the order category exactly as before.

## 7. Tests

New file: `Integration.NS.Tests/Payloads/PackingIFPayloadDTOTests.cs` — 20 cases covering:

- All six (condition × category) combinations for Returns.
- The 3a regression guard: bad line on a Returns-Good order must serialise as `3`.
- Transfer Order and VRA mappings, including Missing.
- **Cross-path agreement:** for a Returns-Good order the packing and receiving payloads
  must serialise the same status for every condition pair. This is the test that fails
  if the two halves drift apart again.
- Zero-quantity filler lines emit no `inventoryStatus`.
- Non-bin locations send no `inventoryDetail`.

```
dotnet test Integration.NS.Tests/Integration.NS.Tests.csproj
Passed! - Failed: 0, Passed: 294, Skipped: 0, Total: 294
```

## 8. Verification

- `dotnet build Integration.NS.Tests` — succeeded, 0 errors, no new warnings.
- `dotnet build Web.BlazorServer` — succeeded, 0 errors.
- Full test suite — 294 passed.

## 9. What this does NOT fix

### 9a. The 400 itself — NetSuite availability

This change corrects *which* status is sent and prevents silent mis-posting. It does
**not** address why NetSuite rejected `3`. With `3` confirmed present in the account,
`Invalid Field Value 3` means the status is **not available in that context**, most
likely item/location availability on the source location.

Confirm before assuming it is fixed:

1. Open the transfer order in NetSuite, build the fulfillment manually, and try to
   select inventory status Bad for that item at the from-location. If the UI will not
   offer it, the constraint is availability and needs a NetSuite configuration change.
2. Corroborate in SuiteQL:

```sql
SELECT i.itemcode, ib.inventorystatus, s.name, SUM(ib.quantityavailable) AS qty
FROM inventorybalance ib
JOIN item i            ON i.id = ib.item
JOIN inventorystatus s ON s.id = ib.inventorystatus
WHERE ib.location = <from_location_id>
  AND ib.item IN (
      SELECT tl.item FROM transactionline tl
      JOIN transferorder t ON t.id = tl.transaction
      WHERE t.tranid = 'TO-xxxxx')
GROUP BY i.itemcode, ib.inventorystatus, s.name
ORDER BY i.itemcode, ib.inventorystatus
```

If Bad never appears for those items at that location, the cause is availability, not
code — and it is worth asking whether bad returns should be bookable from that location
at all.

### 9b. Mobile cannot mark damage — open product decision

`Packing/Returns/ReturnsItemView.razor.cs:121` hardcodes `IsBad = false` and the screen
has no Good/Bad toggle, unlike `Packing/TransferOrder/TransferOrderItemView.razor.cs:54`
(`NextScanIsBad`, splitting into `GoodTOItems`/`BadTOItems` at `:133`).

Consequence: on mobile, damage can only be expressed through the order-level transfer
category. A single bad line on an otherwise-good return is still unrepresentable there.

**This fix was deliberately scoped not to add that toggle**, because it is a UX/product
decision rather than a defect: it changes what warehouse operators can do on a handheld,
and may require process sign-off. Web is unaffected — it already supports bad quantities.

Recommended follow-up: port the `NextScanIsBad` pattern to mobile Packing Returns, then
drop the `transferCategory` argument from the packing calls entirely.

### 9c. Still open from the 4xx catalog

- **1a / 2a — missing bin.** `BinNumber` still resolves to `null` when both bin IDs are
  `0` on a bin-tracked location, producing `Please configure the inventory details in
  line N`. Needs a fail-closed guard.
- **1b / 2b — duplicate `orderLine`** on a good+bad split scan. `ReturnsIFPayloadDTO.cs:28`
  is a bare `.Select(...)` with no `GroupBy`. `PurchaseOrderIRPayloadDTO.cs:42` already
  groups and is the reference.
- **Lot/serial-tracked items.** `InventoryAssignmentItem` (`PayloadsDTO.cs:67-77`) has no
  lot or serial fields at all, so those items cannot satisfy NetSuite's inventory-detail
  requirement today.
- **6c — raw error leakage.** `PostReturnsIFCmd.cs:60` returns `ex.Message` verbatim, so
  the `[diag replace=… usedBin=…]` string from `NetSuiteApiClientService.cs:1150` is
  rendered on operator-facing screens on both web and mobile. Log the diag, show a clean
  message plus a correlation id.

## 10. Hardcoded account IDs elsewhere

`3` is not the only account-specific literal. `PostInventoryWorksheetPayloadDTO` hardcodes
`account = 1`, `department = 15`, `class = 2`. These will fail the same way on a second
subsidiary. A runtime resolver was deliberately **not** built for the status IDs — with
verified ground truth for one account, hardcoded constants plus a test are sufficient.
Build the resolver only when a second account or subsidiary is onboarded, and build it
once for both cases.