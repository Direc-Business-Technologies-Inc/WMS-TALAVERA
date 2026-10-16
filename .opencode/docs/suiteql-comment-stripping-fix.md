# SuiteQL Comment Stripping — Packing Lines Could Not Load (2026-10-16)

**Incident:** opening a packing order (`/transactions/delivery/packing/stock-transfer-request/view?ref=ISTR-THM00100000648`)
rendered the header but failed the Lines grid with

```text
Get Packing Stock Transfer Request Lines failed: NetSuite 400 Bad Request: Bad
Request(400): [INVALID_PARAMETER] Invalid search query. Detailed unprocessed
description follows. Search error occurred: Failed to parse SQL [...]:
end of input, state:96(10102) near:  (0,0, token code:0)
no_root_node(-1000) near: no root node(0,0, token code:0).
```

The toast truncates before the token detail. The decisive evidence is the request
body logged by `NetSuiteApiClientService.LogNonSuccessResponse` — read that before
forming a hypothesis.

Related reading: `.opencode/docs/SuiteQL-Shape-Rules.md` (Rule 7),
`.opencode/docs/SuiteQL-Robustness-Changes.md` (the `SuiteQLText` /
`SuiteQLAssert` chokepoints this fix builds on).

---

## Cause

`NetSuiteApiClientService.FormatQuery` collapsed all whitespace before sending:

```csharp
string FormatQuery(string query) => Regex.Replace(query, @"\s+", " ").Trim();
```

`\s+` includes `\n`. Scripts are read from disk with newlines intact
(`SqlQueryManager.GetSqlScriptWithMetadata` uses `ReadToEnd`), so each `--` line
comment in a `.sql` file became a comment running to the end of the flattened
statement. The payload NetSuite received looked like this:

```text
... JOIN unitstypeuom uom ON tl.units = uom.internalid -- One row per (item,
location) / (item, subsidiary, location): without GROUP BY these -- joins fan
out ... -- and the duplicated orderLine breaks the itemFulfillment transform at
save time. LEFT JOIN ( SELECT ibq.item, MIN(ibq.bin) AS bin, ... ) q
```

NetSuite parsed only as far as `uom.internalid`. The `WHERE`, every subsequent
join, and the closing `) q` were all inside the comment. Hence `end of input`
with no root node — a truncated statement, not a malformed one.

Three scripts were affected, and they were the **only** three files in
`NSScripts` containing comments:

- `NS_TO_x_Packing_Get_Items.sql`
- `NS_TO_x_Return_x_Packing_Get_Items.sql`
- `NS_VendorReturnAuthorization_Get_Items.sql`

Both call paths were affected: `NetsuiteQuery` (`:889`, mobile) and
`ExecuteSuiteQLQuery` (`:903`, web).

---

## Fix

### 1. `SuiteQLText.StripComments` — `Integration.NS/Helpers/SuiteQLText.cs`

Character scanner removing `--` and `/* */`. String-literal aware, mirroring
`SuiteQLAssert.StripLiterals`: a doubled `''` is consumed as an escape and does
not close the literal, so a `TO_CHAR` format or barcode value containing `--`
survives. Newlines terminating a line comment are preserved so the next
statement is not joined onto it. Returns the input by reference when the query
contains neither marker.

### 2. Wired into the collapse — `NetSuiteApiClientService.cs`

```csharp
string FormatQuery(string query)
    => Regex.Replace(Helpers.SuiteQLText.StripComments(query), @"\s+", " ").Trim();
```

Order is load-bearing: stripping after the collapse is a no-op.

### 3. Scripts cleaned

Comment blocks deleted; their rationale moved to XML doc comments on the three
consuming methods (`GetPackingStockTransferRequestLines`,
`GetPackingReturnLines`, `GetPackingVendorReturnAuthorizationLines`), where it
cannot reach NetSuite.

### 4. Tests — `Integration.NS.Tests/Services/SuiteQLTextCommentTests.cs`

Line comments, block comments, unterminated block comments, `--` inside a
literal, doubled-quote handling, identity for marker-free input, and the
production shape (a flattened query keeping its `WHERE` and balanced parens).
Plus repo-wide guards: no shipped script may carry a comment, and every script
must stay well-formed after stripping. `SuiteQLAssert.HasComment` was added to
`SuiteQLAssert` for the first of these.

---

## Diagnosis note — a hypothesis that was wrong

`ORDER BY` inside the `AggregateItemLocation` derived table was the leading
suspect, on the reasoning that it is not legal SQL in a joined subquery. It was
removed during the fix, then restored and retested on 2026-10-16: **the query
succeeds either way.** NetSuite accepts it. That removal was unnecessary churn
and should stay reverted.

The real defect was only findable from the logged request body, where the
flattened comment is visible. Next time, read `Request payload:` in the warning
log before forming a hypothesis.

---

## Open items

- **Test duplication.** `SuiteQLTextCommentTests` re-implements the `NSScripts`
  directory walk already present in
  `Integration.NS.Tests/Services/NSScriptsWellFormedTests.cs`. Consolidate: fold
  the two repo-wide guards into that class as additional `[Theory]` methods and
  delete the duplicate locator. Nothing is broken today; it is just two sources
  of truth.
- **Coverage gap.** `No_shipping_script_carries_a_sql_comment` only scans `.sql`
  files. Queries assembled in C# through `SuiteQLQueryBuilder` are not covered,
  so a commented-out fragment there would vanish silently. Extending the same
  check over captured builder queries is cheap — `CapturingNetSuiteClient` already
  exists — but was not done.
- **Existing locator prefers the build output.**
  `NSScriptsWellFormedTests.ScriptsDirectory` reads `<base>/Scripts` (the copy
  NetSuite actually receives) before falling back to source. That is arguably
  more faithful, but it means the test validates a stale copy if source is edited
  without rebuilding. This bit us during the incident: an `ORDER BY` edit was made
  in `bin/` only, so `git diff` showed the source unchanged. Edit scripts in
  `Integration.NS/NSScripts`.
- **Pre-existing suite flakiness, unrelated to this fix.** Full-suite runs vary:
  `GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks` (timing
  based) and
  `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`
  (observed 30s) fail intermittently and pass in isolation. The second is
  notable: `SuiteQL-Robustness-Changes.md` §9 records that exact test as fixed by
  `NetSuiteApiClientService.ResetHttpClientForTests()`, which still exists and is
  still called from `Dispose` — so whatever mutates the shared static
  `HttpClient` is not fully cleaned up. Worth its own ticket.

---

## Verification

- `dotnet build Integration.NS` — 0 errors; all warnings pre-existing.
- Targeted suite (comment, resolver, STR packing) — 17/17, three consecutive runs.
- Full `Integration.NS.Tests` — 325 tests; results vary by the flake above
  (325/325 green on clean runs).
- Manual: opening `ISTR-THM00100000648` loads the Lines grid with no NetSuite
  400. The Returns and Returns-to-Supplier tabs use the same corrected mechanism
  and should be spot-checked.