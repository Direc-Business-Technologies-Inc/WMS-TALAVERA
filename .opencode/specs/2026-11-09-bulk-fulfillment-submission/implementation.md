# Implementation notes

## User clarifications superseding prerequisites

- The user confirmed the business input: current date in `MM/DD/YYYY`, Submitted status, and an array of RTS internal IDs. The REST wire representation is corrected below following the reported `400 INVALID_CONTENT` response.
- The user requested the existing codebase timezone convention. `Domain.Providers.DateTimeProvider.Today` delegates to `DateTime.Today` (server-local date); the implementation deliberately uses this existing provider. This supersedes the earlier requirement to introduce an explicitly agreed timezone.
- Submitted is mapped to internal ID `1` from the approved spec.
- The user instructed: no branch changes, commits, or pushes.

## Implementation decisions

- Presentation enforces the existing `permission.OSRN.CREATE` policy (the Supplier Return create permission).
- Allowed subsidiary IDs are resolved from the authenticated server-side `com.direcbusiness.wms.nsAllowedSubsidiaries` claim. Missing, empty, invalid, or nonpositive scope fails closed for this operation.
- An uncached SuiteQL lookup checks every selected internal ID is a `vendorreturnauthorization` in that scope. The confirmed set must exactly match the result set before posting.
- The grid copies selection IDs and display fields before confirmation, and guards the entire dialog/submission lifecycle. Dialog search changes display only.
- One POST targets `/record/v1/customrecord_bulk_fulfillment`, with only date, status, and unique RTS IDs. No IF writes are invoked.
- Creation receipts preserve HTTP status and Location before attempting a body read. Absolute/relative locations are checked for the expected record type and positive integer ID. Missing/malformed references produce accepted-without-reference feedback, never another POST.
- Existing write-safe 429 retries remain intact. Network/timeout/ambiguous server failures produce an uncertain result requiring NetSuite verification before resubmission.
- Invalid selection, denied access, failed pre-write validation, definite rejection, and uncertainty preserve the current selection. Accepted requests clear it.

## Verification

- `dotnet build "WMS.slnx" --no-restore --verbosity quiet`: passed, 0 errors (existing solution warnings). Initial full build exceeded its 200-second tool timeout; the subsequent build completed in approximately 4 minutes.
- `dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore --verbosity quiet`: 840 passed.
- `dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj" --no-restore --verbosity quiet`: 82 passed, including permission/scope enforcement, cancellation, minimum selection, guard through dialog/request, search invariance, and selection outcome behavior.
- `git diff --check`: passed.
- `graphify update .`: completed; SQL extraction reported its existing missing `tree_sitter_sql` dependency.
- Local tests use existing test doubles and WireMock; no live NetSuite writes were made. Target-account field compatibility, role permissions, mandatory fields, and duplicate behavior still require authorized sandbox verification. The user-provided input format is an implementation contract, not recorded sandbox evidence.
- Duplicate prevention remains local to the component's in-flight workflow; no durable cross-session idempotency guarantee is introduced.

## Correction: REST wire representation after INVALID_CONTENT

The reported live response was `400 INVALID_CONTENT` for a slash-formatted date, raw integer status, and raw integer-array multi-select. The original tests repeated that incorrect wire-format assumption.

The payload now serializes a `DateOnly` as ISO `yyyy-MM-dd`, status as an ID reference, and the multi-select as an `items` collection of ID references. The business date still comes from the existing server-local provider. For example:

```json
{
  "custrecord_bf_date": "2026-11-09",
  "custrecord_bf_status": { "id": "1" },
  "custrecord_bf_return_to_supplier_ids": {
    "items": [{ "id": "1234" }, { "id": "5678" }]
  }
}
```

Oracle's [Using Datetime Fields](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_157305090645.html) specifies REST full-date notation. [Custom-record CRUD guidance](https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/section_158100110171.html) applies standard REST record conventions to custom records. Account-specific metadata and successful live acceptance have not been captured; no live POST was made to validate this correction.

The rejection message now points to the exact response in server logs instead of suggesting permissions are the established cause.

Correction verification:
- Integration project build: passed, zero warnings/errors on the final incremental build.
- Integration test suite: 841 passed, including the corrected end-to-end payload assertions and regression coverage for the six IDs in the reported failure.
