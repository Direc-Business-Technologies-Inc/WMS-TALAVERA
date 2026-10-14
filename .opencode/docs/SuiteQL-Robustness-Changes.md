# SuiteQL Robustness — Change Record (2026-10-13)

**Incident:** Receiving → STR → Create Item Receipt failed with
`NetSuite 400 Bad Request: [INVALID_PARAMETER] Invalid search query ... Search error occurred:
Invalid or unsupported search`. Recurring historically, appearing user-specific.

**User-specificity explained:** the error was actually *path*-specific — only the Item
Fulfillment branch of the item-receipt load was affected, which only the STR/Returns web
flow reaches. PO receipts and mobile never executed the broken query shape.

Related reading: `.opencode/docs/SuiteQL-Shape-Rules.md` (the enforced rule set),
`.opencode/specs/2026-10-13-returns-receipt-open-quantity-divergence/` (the spec whose
QuantityReceived change triggered the deterministic failure).

---

## Session 1 — Root-cause fix and first hardening

### 1. De-correlate the IF-branch line query — `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`

**Defect:** `GetItemReceiptItemFulfillmentLinesAsync` built its SELECT list with two
correlated scalar subqueries:

- `LineNumber` = `IF_TO_LINE_ID` — referenced outer alias `tl` from inside a 3-table subquery.
- `QuantityReceived` = `IF_TO_LINE_RECEIVED_QTY` — wrapped `IF_TO_LINE_ID` inside another
  subquery, reaching the outer `uom.conversionrate` two levels deep.

NetSuite's SuiteQL optimizer rejects that shape with the 400 above. The PO path
(`GetItemReceiptLinesAsync`) reads `tl.quantityshiprecv` directly — no correlation — which is
why it always worked.

**Fix:** both constants replaced by `IF_TO_LINE_MAP`, one derived table that resolves the
IF → TO RECEIVING line mapping entirely inside itself, LEFT JOINed on its own output
columns — the same shape as the production-proven `pb` (preferred-bin) join in the same
query:

```sql
LEFT JOIN (
    SELECT tolink.nextdoc AS ifdoc, tolink.nextline AS ifline,
           ttl.id AS tolineid, ttl.quantityshiprecv AS qtyrecv
    FROM previoustransactionlinelink tolink
    JOIN transactionline shippingline ON ...
    JOIN transactionline ttl ON ...
    WHERE ttl.transactionlinetype = 'RECEIVING' AND tolink.previoustype = 'TrnfrOrd'
) ifmap ON ifmap.ifdoc = tl.transaction AND ifmap.ifline = tl.id
```

- `LineNumber` = `ifmap.tolineid` (same TO RECEIVING line id as before).
- `QuantityReceived` = `NVL(ifmap.qtyrecv / uom.conversionrate, 0)` (identical ledger source;
  the redundant `rline` self-join was dropped because `rline.id = ttl.id`).
- The 2026-10-13 over-receipt design keeps its semantics; only the delivery mechanism changed.

### 2. Escape every interpolated string literal — `Integration.NS/Services/SuiteQLQueryBuilder.cs`

`_stringifyValue` (Equals/GreaterThan/In/… values) and `_stringOp` (Contains/StartsWith/
EndsWith grid filters) now double single quotes before interpolation. Previously a tranid or
user-typed filter containing an apostrophe produced unbalanced-literal SQL — a second
member of the same 400 family. Raw SQL remains deliberately unescaped via
`SuiteQLQueryBuilder.Literal`.

### 3. Classified, self-diagnosing errors — `Integration.NS/Exceptions/*`, `Integration.NS/Services/NetSuiteApiClientService.cs`

- New `NetSuiteErrorCode.UnsupportedSearch` (matches "invalid or unsupported search";
  classified before the offset rule so wording drift can never re-map it).
- `NetSuiteExceptionFactory.CreateForStatus` gained an optional `requestBody`. For
  `UnsupportedSearch` failures it appends
  `Failing query (excerpt): …` (max 500 chars, extracted from the request body's `q`) to the
  exception message — the failing shape is visible from the toast/log without grepping
  request-body logs. All other messages are byte-identical to before.
- Wired at both throw sites (`SendAndHandleOAuth2`, `SendAndHandleOAuth1`).

### 4. Rule book + characterization tests

- `.opencode/docs/SuiteQL-Shape-Rules.md` — five rules, each with accepted/rejected shapes
  and the incident that taught it, plus the "new 4xx → rule + test" loop.
- `Integration.NS.Tests/Implementations/Transactions/GetItemReceiptItemFulfillmentLinesQueryShapeTests.cs`
  — five tests asserting the SELECT list contains no scalar subqueries and the derived table
  resolves everything internally (whitespace-normalized asserts).
- `Integration.NS.Tests/Services/SuiteQLLiteralEscapingTests.cs` — Equals/In/Contains/
  StartsWith/EndsWith escaping matrix.
- `Integration.NS.Tests/Exceptions/NetSuiteExceptionFactoryTests.cs` — classification from
  the real production wording, excerpt append/truncation, message-unchanged guarantees.

---

## Session 2 — Named-script chokepoint (after analysis review)

### 5. Review verdict on the submitted analysis

The analysis's structural map was correct (two query chokepoints; ~30 named-script call
sites; duplicated substitution logic; zero test coverage via
`CapturingNetSuiteClient.NetsuiteQuery` throwing `NotSupportedException`). Its headline
diagnosis was wrong:

- `NS_Item_Get_PerBarcode.sql:16` uses `bc.name = @barcode` — the **escaped** `@` form
  (`NetSuiteApiClientService` escapes `@key` values). The "scanned barcode with an
  apostrophe → unbalanced quote" vector does not exist.
- The `{key}` raw-splice form is used by exactly two scripts — `NS_Item_Get_Barcodes.sql:16`
  (`i.id IN ({items})`) and `NS_Location_Get_Bins.sql:7` (`b.location IN ({locations})`) —
  both splicing comma-joined **integer** ids (`ItemBarcodesRequestDTO.NetsuiteMaterialInternalId`,
  `BinLocationRequestDTO.Location` are `int`).
- The **real** defect in that path: an empty selection yields `IN ()` → 400
  INVALID_PARAMETER (data-shape dependent, so it also looks "user-specific"), and the raw
  splice accepts arbitrary text — no guard between user data and SQL.

### 6. `SuiteQLText` — one substitution chokepoint, guarded raw splice
`Integration.NS/Helpers/SuiteQLText.cs` (internal)

- `EscapeLiteral` / `Literal` — single source of quote escaping.
- `SubstituteScriptParameters` — the two-form substitution both paths now share:
  - `@key` → quoted, escaped string literal (behavior unchanged).
  - `{key}` → validated **only when the token is actually present**: must be a non-empty,
    comma-separated integer list (`^\d+(\s*,\s*\d+)*$`, whitespace-tolerant, trimmed),
    else a local `InvalidOperationException` names the parameter. This kills the `IN ()`
    class and closes the raw-splice injection door without escaping the existing int-list
    scripts (escaping them would produce `IN ('1,2,3')` — the exact trade-off the analysis
    warned about, confirmed real).
- `Integration.NS/Services/NetSuiteApiClientService.cs` (`NetsuiteQuery`) and
  `Integration.NS/Helpers/SuiteQLScriptResolver.cs` now delegate to
  `SubstituteScriptParameters` — the duplicated hand-rolled loops are deleted.
- `SuiteQLQueryBuilder` now calls `SuiteQLText.EscapeLiteral` instead of its own private
  copy — one escaping implementation for the whole integration layer.

### 7. `SuiteQLAssert` — shared invariant library
`Integration.NS.Tests/Asserts/SuiteQLAssert.cs`

One call proves a query is structurally well-formed regardless of which chokepoint built it:

1. string literals closed (stateful scan; doubled `''` consumed — catches unescaped
   apostrophes);
2. parentheses balanced (literal contents excluded from the count);
3. no empty `IN ()`;
4. no unsubstituted `@key` / `{key}` tokens (literal contents excluded, so data containing
   `@` or `{` cannot trip it);
5. limit `null` or 1..1000;
6. offset non-negative and divisible by limit when both present.

Also `WellFormed(SuiteQLQuery)` overload for builder output. These invariants prove
well-formedness, not semantic acceptance — the sandbox canary remains deferred until
credentials exist (SuiteQL-Shape-Rules.md).

### 8. Chokepoint matrix + resolver e2e tests

- `Integration.NS.Tests/Services/SuiteQLScriptSubstitutionTests.cs` — input-class matrix
  (plain, apostrophe, injection, empty, unicode, leading zeros, int lists, whitespace) over
  `SuiteQLText`; asserts `@param` escapes every class, `{param}` splices/spits/throws per
  class, `{param}` validation never fires when the token is absent, and both forms can coexist.
- `Integration.NS.Tests/Services/SuiteQLScriptResolverTests.cs` — end-to-end through
  `SuiteQLScriptResolver.ResolveSuiteQLScript` with a fake `ISqlQueryManager`: resolved
  scripts pass every `SuiteQLAssert` invariant, raw-script passthrough, missing-script error,
  and the empty-`{locations}` case failing locally instead of at NetSuite.

### 9. Pre-existing test-infra leak fixed (found while verifying)

`NetSuiteApiClientServiceResilienceTests` replaced the shared static `HttpClient`
(WireMock client; 30-second stubs at `:147-207`) and never restored it, intermittently
failing `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`
in full-suite runs. Added `NetSuiteApiClientService.ResetHttpClientForTests()` (rebuilds via
`CreateHttpClient()`) and call it in `Dispose`.

---

## Verification

- `dotnet build` on `Integration.NS` and `Integration.NS.Tests`: 0 errors (all warnings pre-existing).
- `dotnet test`: **234/234 passing**, stable across repeated full runs.
  (One unrelated `GridResultCacheTests` eviction-timing flake appeared once across many runs;
  passes in isolation and on re-run — pre-existing `MemoryCache` compaction timing, unchanged code.)
- Sandbox confirmation of the reshaped IF query against `IF-THM00100000660` remains a manual
  pre-deploy step (no sandbox credentials).

## Deliberately deferred

- **Sandbox smoke canary** (executes every harvested query at `limit=1` against the sandbox) — needs sandbox credentials; the manual equivalent is documented in SuiteQL-Shape-Rules.md.
- **Layer 3b** — hostile-`DataGridIntent` sweep across all grid methods (one hostile intent, ~19 integrations, assert `SuiteQLAssert.WellFormed` on everything captured).
- **Layer 3** — env-gated production query capture; `LogNonSuccessResponse` already yields the exact query for the next incident.

## Incident loop (going forward)

Every future NetSuite 4xx: (1) capture the query from the warning log or the new
`Failing query (excerpt)` in the exception; (2) reproduce in the NetSuite SuiteQL console;
(3) if a new shape violation, add it to `SuiteQL-Shape-Rules.md`; (4) add a characterization
test that fails when the bad shape reappears.
