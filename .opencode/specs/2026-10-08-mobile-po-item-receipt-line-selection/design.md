# Design — Mobile PO Item Receipt: Explicit Line Selection in Payload

**Date:** 2026-10-08
**Status:** Confirmed
**Mode:** BUGFIX
**Scope:** `Integration.NS` mobile PO receipt posting + `Application.DataTransferObjects` payload factory
**Related:** `fix-netsuite-item-receipt-error-visibility` (implemented; this spec assumes its G1 payload logging exists)

## Decision Record

**Problem:** Mobile PO item receipt payloads omit unselected lines; NetSuite's PO→ItemReceipt transform defaults omitted lines to `itemreceive = true`, causing `[USER_ERROR] Please configure the inventory detail in line N` on bin/lot-tracked items and silent over-receipt on non-tracked items.

**Decisions:**

1. **Emit every line with explicit `itemreceive`** — matches the proven web path (`ReceivingIntegration.cs:971-999`). *Trade-off:* larger payloads vs. correctness; NetSuite offers no "don't receive by default" switch.
2. **Keep the public `CreateForItemReceipt` signature** but pass the **full** line list; the factory derives the received-line predicate from `receivingCategory` (1 → `!IsBad && ScannedQuantity > 0`, 2 → `IsBad && ScannedQuantity > 0`). *Trade-off:* avoids touching the `INetSuiteApiClientService` contract and `PostPurchaseOrderIRCmd`; cost is the factory now knows category semantics — accepted because it already owns category-specific field logic (`Rate = line.IsBad ? 0 : null`).
3. **Keep bucket-skip guards in `SavePOItemReceipt`** (`badPO.Any()` / `goodPO.Any()` decide *whether* to post), but feed both posts the full line list. *Trade-off:* none material; preserves "no empty receipt posts" behavior.
4. **Preserve `inventoryStatus = "1"` in the BAD post** (user-confirmed Option A). Status alignment with web (status 3) is deferred to a follow-up spec.

## What Already Exists

| Piece | Location | Status |
|---|---|---|
| Mobile PO post orchestrator | `Integration.NS/Services/NetSuiteApiClientService.cs:884-927` (`SavePOItemReceipt`) | Pre-filters lines at `:890`, `:908` — **changes** |
| Payload factory | `Application.DataTransferObjects/Transactions/Receiving/NS/PurchaseOrderIRPayloadDTO.cs:23-139` | Emits only passed lines; dead zero-qty branch at `:40-47` — **changes** |
| Line DTO | `Application.DataTransferObjects/Transactions/Receiving/NS/PostPurchaseOrderDTO.cs` (`IsBad:50`, `ScannedQuantity:47`, `LineSequenceNumber:24`) | Unchanged |
| Shared payload shapes | `Application.DataTransferObjects/Transactions/Commons/NS/Payload/PayloadsDTO.cs` (`OrderLineItem`, `InventoryDetail`, …) | Unchanged |
| Serializer options (null-dropping) | `Integration.NS/Services/NetSuiteApiClientService.cs:181-186` | Unchanged |
| MediatR entry | `Application.UseCases/Commands/Transaction/Receiving/NS/PurchaseOrder/PostPurchaseOrderIRCmd.cs:17` | Unchanged |
| Web reference implementation | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:952-1005` (`CreatePOJson`) | Unchanged, reference only |
| Test project | `Integration.NS.Tests` (xUnit, sequential) | **Extended** — new payload tests |
| Existing fake | `Integration.NS.Tests/Fakes/CountingNetSuiteClient.cs:67` | Unchanged |

**What changes:** `SavePOItemReceipt` (filter logic only) and `PurchaseOrderIRPayloadDTO` (line emission). **What does not:** URLs, auth, `MakeRequest`, DTO contracts, MediatR flow, web path.

## Overview

```mermaid
sequenceDiagram
    participant Cmd as PostPurchaseOrderIRCmd
    participant Svc as NetSuiteApiClientService
    participant NS as NetSuite REST

    Cmd->>Svc: SavePOItemReceipt(allLines, userId, remarks)
    Note over Svc: bucket guards: hasBad = any(IsBad && qty>0), hasGood = any(!IsBad && qty>0)
    alt hasBad
        Svc->>Svc: CreateForItemReceipt(allLines, cat=2)
        Note over Svc: bad lines itemreceive=true;<br/>good & zero-qty lines itemreceive=false
        Svc->>NS: POST purchaseOrder/{id}/!transform/itemReceipt
    end
    alt hasGood
        Svc->>Svc: CreateForItemReceipt(allLines, cat=1)
        Note over Svc: good lines itemreceive=true;<br/>bad & zero-qty lines itemreceive=false
        Svc->>NS: POST purchaseOrder/{id}/!transform/itemReceipt
    end
```

## Root Cause (evidence-backed)

- `NetSuiteApiClientService.cs:890,908` filters `ScannedQuantity > 0` per bucket before payload construction; `PurchaseOrderIRPayloadDTO.cs:38-136` emits only those lines.
- NetSuite transform semantics: lines absent from `item.items` default to `itemreceive = true`.
- Web path avoids this by mapping all lines with explicit `itemreceive` (`ReceivingIntegration.cs:978`).
- Commit `29a6a21` ("BinNumber assignment logic") touched bin resolution only — it could not fix this because the defect is line *membership*, not bin values.

**Bug-condition example:** PO with lines L1 (good, qty 10, bin-tracked), L2 (zero scanned), L3 (bad, qty 2). GOOD post today sends only L1 → NetSuite default-receives L2 and L3 → L3 is bin-tracked → error cites "line 2" (L2's position in NetSuite's item list). Non-bin-tracked variant: no error, but L2/L3 silently received at full open qty inside a Good-category receipt.

## Correctness Properties

- **P1 (→2.1):** For any input list, `item.items` contains exactly one entry per input line, keyed by `orderLine = LineSequenceNumber`.
- **P2 (→2.2):** Every entry where the line is not in the post's bucket serializes with `itemreceive: false` and **no** `quantity`, `inventoryDetail`, `rate`, `custcol_dbti_record_weight`, `custcol_dbti_actual_weight`, `custcol_dbti_actual_qty`, or `location` (null-dropped by `JsonIgnoreCondition.WhenWritingNull`).
- **P3 (→2.3):** Bucket-line entries are field-identical to the current implementation (orderLine, quantity, weights, `custcol_dbti_actual_qty`, rate rule, inventoryDetail shape, `inventoryStatus = "1"`, binNumber resolution chain).
- **P4 (→2.4):** Single-line, fully-received PO → payload identical to current output.
- **P5 (→2.5):** For the same input list, the set of `itemreceive = true` lines in the GOOD payload and in the BAD payload are disjoint and their union equals all lines with `ScannedQuantity > 0`.
- **P6 (→2.6):** `SavePOItemReceipt` returns `true` on success; throws with the existing `Error while posting GOOD/BAD PO Item Receipt.` wrapping on failure; posts nothing when a bucket is empty.

## Error Handling

Unchanged: per-post try/catch rewraps at `NetSuiteApiClientService.cs:904,922`; BAD post failure still aborts the flow before the GOOD post (existing sequential behavior preserved). Payload logging from the prior spec continues to capture full JSON for diagnosis.

## Testing Strategy

- **Unit (new, `Integration.NS.Tests`):** `PurchaseOrderIRPayloadDTOTests` — pure factory tests, no HTTP. Serialize with options mirroring `NetSuiteApiClientService.cs:181-186` (`WhenWritingNull`) and assert raw JSON so null-dropping is covered. Cases: mixed good/bad/zero-qty PO (P1, P2, P5), single-line PO (P4), field-shape assertions on received lines (P3), empty-bucket list.
- **Unit (existing pattern):** guard logic in `SavePOItemReceipt` is private; covered indirectly via P6 through a manual mixed-PO verification or left to integration (no HTTP mock harness exists for `MakeRequest` — stated limitation).
- **Integration/manual:** one mobile receipt on a mixed PO (good + bad + unscanned line, bin-tracked item) against the staging NetSuite account; verify in NetSuite UI that the GOOD receipt contains only good lines and the BAD receipt only bad lines.
- **Regression:** `dotnet build` + `dotnet test` on `Integration.NS.Tests`.

**Property-based testing assessment:** NOT APPLICABLE — the factory is a small deterministic mapping with ~3 input dimensions; example-based tests cover the partition fully. No property-testing framework exists in the repo.

## Accepted Consequences

- Payloads carry all lines (larger JSON) — accepted; web path already does this.
- The factory now interprets `receivingCategory` semantics — accepted per Decision 2.
- `inventoryStatus = "1"` in BAD posts persists — accepted (Option A); follow-up spec recommended.

## Out of Scope

Web path, TO/Returns flows, `inventoryStatus` alignment, `binNumber` fallback, `AlreadyFulfilled` field usage, all architectural debts.

## Verification Checklist

1. `dotnet build` — clean.
2. `dotnet test` — all `Integration.NS.Tests` pass including new payload tests.
3. Manual mixed-PO mobile receipt — GOOD and BAD receipts post without the inventory-detail error.
4. NetSuite UI — each receipt shows only its bucket's lines; quantities match scans.
