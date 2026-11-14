# Mobile IF stabilization — tester guide

Date: 2026-11-09  
Status: Acceptance scenarios; device/NetSuite results not yet recorded  
Scope: STR, Returns, and RTS Packing  
Implementation: [mobile-if-stabilization.md](mobile-if-stabilization.md)

## Test setup

- Use the intended test API and NetSuite sandbox, with source orders that have
  remaining fulfillment quantities.
- Record APK version, API build, source order, vendor where applicable, quality,
  expected quantity and created fulfillment IDs.
- Your sample APK's manual-entry configuration remains as configured. You can
  use it to establish quantities for the save/cancellation tests. These scenarios
  do not assess manual-entry eligibility or quantity-validation rules.
- Scanner timing cases require a scanner-enabled device or the normal barcode
  broadcast test setup. Manual entry alone cannot validate hardware delivery.
- The APK fixes and API preflight fixes have separate deployment boundaries.
  Confirm which server build the APK is using before testing backend behavior.

## APK/save-state checks

| # | Steps | Expected result |
| --- | --- | --- |
| 1 | STR: establish quantity 1; use Change Vendor → Save now without ordinary Save first | Fulfillment contains the current quantity; vendor changes only after success |
| 2 | STR: establish quantity 1; press Save and cancel; increase to 2; Change Vendor → Save now | Quantity 2 is submitted, not an old quantity-1 snapshot |
| 3 | Returns: establish quantity 1; Save → Cancel; increase to 2; inspect displayed quantity and save | Display and submitted quantity both equal 2; cancellation did not replace the live rows |
| 4 | Each flow: cancel Save confirmation, then edit/scan and Save again | No first POST; editing and the next Save still work |
| 5 | Each flow: delay a save response; press Save again / attempt refresh; on STR attempt Change Vendor | No second save or overlapping refresh/vendor change; wait/retry feedback is shown |
| 6 | Each flow: save with a known rejected request or failed response | Scanned quantities stay available and dirty; STR does not switch vendors |
| 7 | STR: select vendor A's item row; switch/discard and choose B; scan B's item | No hidden vendor-A row selection prevents finding B's item |
| 8 | STR: Good 8 + Bad 6 against plan 20; change fresh remaining to 10 and refresh | Both counts remain 8/6; combined excess warning appears; Save does not post until corrected |
| 9 | STR: scan a line, make it unavailable/closed on the source, refresh | Its positive scans remain visible in Done with a review badge; Save does not silently submit other lines only |
| 10 | Interrupt an item refresh so it fails, then inspect scans and retry | Existing quantities are retained and subsequent operations work |

For case 9, Done is read-only. Reconcile the physical/source discrepancy, then
explicitly discard that vendor's scans using Change Vendor and reload. Do not
treat the review badge as evidence that those scans were posted.

## Scanner timing checks

| # | Steps | Expected result |
| --- | --- | --- |
| 11 | Enable the appropriate Good/Bad scan mode. Start Save; scan another item during confirmation and during delayed HTTP response | The new scan is refused with “scan not accepted” feedback; it does not alter the saved or displayed quantity. Rescan after the operation |
| 12 | Start a barcode lookup on a delayed connection, then immediately press Save | Save asks to wait/retry; after the scan completes, its quantity is visible and dirty. A subsequent Save includes it |
| 13 | Start a move/remove barcode lookup, then Save or refresh | The operation is refused until the edit finishes; quantity changes cannot cross the save snapshot |

Run timing checks in STR, Returns and RTS. Record actual scanner model and delivery
mechanism; successful bUnit dispatch tests do not establish Android delivery.

## API/preflight checks

These are already covered with offline transport tests. A sandbox run can confirm
target-account behavior after the API build is deployed.

| # | Steps | Expected result |
| --- | --- | --- |
| 14 | Submit a mixed-order TO request whose rows share line number and quality; repeat for Returns | Request rejected before fulfillment posting; dedupe does not hide the conflict |
| 15 | Bin-tracked TO/RTS: valid positive BAD item and different positive GOOD item without any resolved bin | Validation error before any BAD/GOOD fulfillment is created |
| 16 | Valid TO with both Good and Bad quantities; repeat for a non-bin location without bin IDs | Both intended buckets still save; non-bin lines are not incorrectly blocked for missing bins |
| 17 | Exempt and non-exempt sources | Existing ship status and print-label behavior remain correct |

If a remote request times out or one quality bucket succeeds before another fails,
check created fulfillments before another attempt. This patch does not provide
durable uncertain-outcome reconciliation or cross-device idempotency.

## Automated commands

```powershell
dotnet test "Mobile.MAUI.Tests/Mobile.MAUI.Tests.csproj"
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --filter "FullyQualifiedName~FulfillmentSaveStabilityTests|FullyQualifiedName~PackingIFPayloadDTOTests|FullyQualifiedName~NetSuiteApiClientServiceTOFulfillment|FullyQualifiedName~NetSuiteApiClientServiceClosedLineGuardTests|FullyQualifiedName~NetSuiteApiClientServiceFulfillmentDedupeTests|FullyQualifiedName~TransferOrderLinePagingTests|FullyQualifiedName~TransferOrderVendorResolverTests|FullyQualifiedName~TransferOrderLineVMPackableQuantityTests"
dotnet build
```

## Result record

| Case | APK/API build | Source order | Expected / actual | Fulfillment IDs | Pass/fail and evidence |
| --- | --- | --- | --- | --- | --- |
| Not run | | | | | |
