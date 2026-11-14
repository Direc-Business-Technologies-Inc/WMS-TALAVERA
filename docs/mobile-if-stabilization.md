# Mobile Item Fulfillment stabilization

Date: 2026-11-09  
Status: Implemented; automated checks passed; device/NetSuite acceptance pending  
Scope: Mobile Packing STR, Returns, and RTS/Vendor Return Authorization save state, with targeted server validation  
Branch created for this work: `claude/fix/mobile-if-stability`

## Purpose

Prevent a mobile fulfillment save from posting an old snapshot, disconnecting the
display from editable data, or accepting scans that cannot be included in the
save. Keep physical scan quantities visible when a refreshed STR plan changes.

This is an implementation record, not a claim that all historical fulfillment
issues are resolved. Manual-entry eligibility, thresholds, quantities, and the
sample APK configuration are outside this change. Row edits participate in the
same operation gate only to prevent them overlapping a save or refresh.

## Defects and fixes

| Defect | Implemented behavior | Owning files |
| --- | --- | --- |
| STR Change Vendor → Save now bypassed snapshot preparation; a previous cancelled Save could leave a stale request | Both ordinary Save and vendor-switch Save build their snapshot inside the same save action immediately before posting | `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs` |
| Returns Save replaced editable rows before confirmation, leaving displayed/selected rows pointing at old objects | Build a separate request list after confirmation. Keep the editable list and its row references intact. Carry backordered quantity into the request | `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsItemView.razor.cs` |
| Hardware broadcasts could mutate quantities while a fixed snapshot was being posted | Dispatch hardware events through Blazor's dispatcher. Hold an edit lease through barcode lookup and application; save/refresh/vendor change requires an exclusive lease. Refuse competing operations with a retry message | All three packing item code-behind files; `Mobile.MAUI/Helpers/PackingSessionGate.cs` |
| Double Save or Change Vendor during another operation could interleave | Hold the exclusive lease from before confirmation until the action and callbacks finish. A competing operation is refused, not queued | All three packing item code-behind files |
| A failed action could leave an earlier successful result visible to a later vendor switch | Clear the packing save action result before execution; continue switching only on the current successful result | STR item code-behind |
| STR refresh independently clamped Good and Bad, potentially leaving their total over the new plan while discarding some scans | Preserve both qualities, report aggregate excess, and refuse Save until excess is corrected. Do not choose which physical quality to discard | STR item code-behind |
| A scanned STR line disappeared from refreshed scannable rows | Retain unmatched positive scans in Done with a review badge and block saving a partial subset | STR item code-behind and markup |
| Vendor change/refresh retained stale row selections | Clear row selections and Last Scanned when the row set changes; preserve current rows on a failed refresh | All three packing item code-behind files |
| Mixed-order validation was removed from TO/Returns handlers; dedupe could hide conflicting identities | Reject the original list before exemption lookup in both commands; validate the original list at the NetSuite save boundary before dedupe, including the direct TO RESTlet method | `PostTransferOrderIFCmd.cs`, `PostReturnsIFCmd.cs`, `Integration.NS/Services/NetSuiteApiClientService.cs` |
| BAD could commit before GOOD failed predictable missing-bin validation | Validate required bins across positive lines before the first TO or RTS bucket write; check both TO bucket line sets before writing | `Integration.NS/Services/NetSuiteApiClientService.cs` |

## Save and scan lifecycle

1. A hardware event is marshalled to the component dispatcher.
2. An accepted scan acquires an edit lease **before** awaiting barcode resolution.
   The lease spans normal, move, and remove scan paths. Dirty state is updated
   before awaiting Last Scanned panel presentation.
3. Save tries to acquire an exclusive operation lease. If an edit/lookup is still
   running, Save returns with a message asking the operator to wait and retry.
   It does not queue a request against an incomplete scan.
4. Once Save owns the lease, further scan events are refused with an explicit
   “scan not accepted” message. This covers confirmation, HTTP, success/failure
   callbacks, and navigation.
5. After confirmation, the save action builds the current request snapshot.
   STR vendor-switch Save uses the same action without a redundant confirmation.
6. Cancelled confirmation performs no snapshot replacement or POST. Failure
   retains editable quantities and dirty state. All exits release the lease.

The gate is per page/session. It is **not** a distributed server lock, a durable
draft, or a NetSuite idempotency mechanism. It does not queue scans made while
busy; the operator must scan those items again after the operation finishes.

## STR refresh conflicts

- Fresh quantities can shrink while another picker works on the same order.
  Existing Good and Bad scans are preserved exactly, including when each fits
  individually but their sum exceeds the new remaining quantity.
- The page warns about excess and prevents posting until it is corrected. The
  existing server aggregate guard remains the final check.
- Scans for unavailable/removed lines remain visible in Done as **Scans need
  review**. Save is blocked rather than silently omitting those scans.
- Done is read-only: reconcile the physical/source-order discrepancy, then use
  **Change Vendor → Discard → confirm** to explicitly reset that vendor's scans
  and reload the appropriate source state. There is no new automatic allocation,
  reconciliation, or discard action.
- A failed item refresh retains existing rows/scans. Selections reset only after
  a successful reload.

## Server contract and deployment

The existing `SaveScan` endpoints and request models remain in use; no database
schema or migration was added.

- TO/Returns commands require one positive internal order ID across the original
  request before resolving exemption.
- TO REST and RESTlet save entry points validate positive scans and single-order
  identity before deduplication. Returns and RTS save entry points validate
  single-order identity before deduplication.
- Bin preflight applies only to positive quantities at bin-tracked locations.
  Non-bin locations and zero-quantity fillers keep their existing behavior.
- REST assignment shape, replace URL behavior, ship status, labels, and the
  established TO reconciliation remain intact.

Publishing the APK deploys the mobile fixes only. The validation/preflight fixes
also require deployment of the API/application/integration assemblies through
the normal API release process. No release APK was signed, installed, or
published as part of this work.

## Verification performed

| Check | Result |
| --- | --- |
| `dotnet test Mobile.MAUI.Tests/Mobile.MAUI.Tests.csproj --no-restore --verbosity minimal` | 12 passed, 0 failed |
| Focused `Integration.NS.Tests` fulfillment suite, including `FulfillmentSaveStabilityTests` | 168 passed, 0 failed; 8 tests are the new service-boundary checks |
| Initial root `dotnet build` | Reached compilation of Android, iOS, Mac Catalyst, Windows and API, but exceeded the 120-second tool timeout |
| Root `dotnet build --no-restore --verbosity minimal` with extended timeout | Succeeded, 0 warnings, 0 errors in the final incremental build |

The mobile tests link the **actual production code-behind**, action runner,
barcode resolver and operation gate into a plain .NET 9 bUnit host. The host
substitutes transport, platform broadcast delivery, toast/JS presentation, and
Razor-generated base declarations. These tests verify workflow logic and event
dispatch, not actual Android scanner delivery or rendered mobile markup. The
solution build separately compiles the real Razor/MAUI project.

The service tests use an in-memory HTTP handler, exercise public save methods,
and assert that invalid GOOD data produces zero fulfillment writes. They do not
contact NetSuite. Existing same-quality dedupe, quantities, filler shape, paging,
vendor resolution, and reconciliation tests were also included.

## Remaining risks and deferred work

1. **Durable posting claims / uncertain outcomes:** the existing workspace removes
   the auto-fulfillment store and its manual posting claims. This change does not
   restore that feature or invent an in-memory replacement with misleading
   durability guarantees. Cross-device/process saves and retry after an ambiguous
   NetSuite result still need a separately designed durable manual posting and
   reconciliation mechanism.
2. **Two outbound writes are not atomic:** all local bin preflight now happens
   first, but GOOD can still fail remotely after BAD succeeds. Reconcile NetSuite
   before retrying an ambiguous/partial result. SQL rollback cannot undo a remote
   fulfillment.
3. **Session expiry and drafts:** no refresh-token or persisted draft recovery was
   added. Actual logout/expiry can still end an unsaved session.
4. **RTS/Returns transform behavior:** this work does not claim to solve every
   historical line-completion or mixed-quality payload concern. Live target-account
   behavior remains an acceptance-test requirement.
5. **Device acceptance:** scanner delivery, busy-overlay behavior, network loss,
   and physical/source reconciliation need the checks in
   [mobile-if-stabilization-testing.md](mobile-if-stabilization-testing.md).

## Related evidence

- [Earlier auth/sublist RCA](../.github/specs/2026-10-14-mobile-auth-logout-and-if-sublist-rca.md)
- [STR line-read analysis](../.opencode/docs/changes/2026-10-22-str-packing-line-read-analysis.md)
- Automated mobile cases: `Mobile.MAUI.Tests/PackingWorkflowTests.cs`
- Automated backend cases: `Integration.NS.Tests/Services/FulfillmentSaveStabilityTests.cs`
