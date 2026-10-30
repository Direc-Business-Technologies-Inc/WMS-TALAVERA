# Web STR receipt picker: large source orders

## Report and confirmed cause

Transfer Order 255540 (`STR-THM00100001039`) has 1,197 receiving lines and 24 completed Item Fulfillments. The web Receiving → Transfer Order → Create Item Receipt picker displayed no IFs. Local logs showed every IF skipped with `Could not verify operational receiving lines: incomplete SuiteQL results` from `GetItemReceiptLinesAsync`.

The web picker verifies each IF using native shipping/receiving-line mappings and IF-linked receipts. It previously fetched metadata for **all** TO receiving lines for each IF. `ReadCompleteAsync` intentionally rejects incomplete SuiteQL results (NetSuite's single page is at most 1,000 rows), so none of the IFs could be verified. Mobile lists IFs using `custbody_dbti_fully_received = F` instead; mobile visibility does not establish a receivable balance.

## Change

- Resolve the IF's native mapping first, then fetch TO receiving-line metadata only for the mapped receiving-line IDs. Missing or duplicate mapping/metadata still fails closed; the picker only offers IFs with a verified positive open quantity.
- The fresh TO-level quantity check before posting now reads the **selected receipt lines** instead of the entire TO ledger. This prevents the same 1,000-row failure after an IF has been selected. An absent ledger line still prevents posting.
- Picker discovery verifies up to three IFs concurrently instead of waiting for all IF verification chains serially. The limit is local to one picker request, not an account-wide NetSuite concurrency guarantee. Individual IF failures still count as skipped; selection and posting recheck the chosen IF. Sorting and paging remain applied after eligibility is established.
- The general `GetItemReceiptLinesAsync` method retains its original full-document behavior for other callers. Single IFs or selected receipts with more than 1,000 relevant lines still fail closed; further paging would be required for those cases.

## Verification

Run `dotnet test Integration.NS.Tests/Integration.NS.Tests.csproj --no-restore --filter "FullyQualifiedName~ReceivingOperationalLinesTests|FullyQualifiedName~ReceivingPostingTests|FullyQualifiedName~GetSTRItemFulfillmentsQueryShapeTests"`.

Regression coverage simulates a 1,197-row source read that would fail without the mapped-line filter; both IF line loading and fresh receipt posting use scoped reads. In a NetSuite sandbox, reopen STR 255540 under Receiving → Transfer Order → Create Item Receipt, confirm `incomplete SuiteQL results` skips disappear, and verify only IFs with genuine open quantities appear. Select one and validate receipt quantities before posting. IFs with no open quantity must remain hidden; do not change receipt flags merely to populate the picker.

For performance validation, measure elapsed time from opening the picker to the first populated grid before and after deployment on the same STR/account. Watch NetSuite 429/concurrency errors and skipped-verification counts. The bounded concurrency reduces serial latency but does not reduce the total number of NetSuite calls; if the account is already near its concurrency limit, lower the bound or implement request-scoped reuse of TO metadata after measuring the slowest stage.

## Slow picker follow-up

The same 24-IF order still took roughly one minute to load after three-way concurrency. Picker verification is a blocking pre-paging pass over every IF, including fully received ones. Added information-level timings per verified IF for operational REST, native mapping, source/metadata, and receipt verification, plus total candidate verification elapsed time. Reopen the picker locally and compare the stage timings and total against the browser wait before choosing a further optimization. Do not substitute `custbody_dbti_fully_received` for verified quantities or increase concurrency without checking account limits.

### Candidate-scoped native link batch

The 24-candidate run took 53,502 ms, with receipt verification of linked IFs commonly taking 2.5–4.4 seconds. Picker discovery now loads native IF→IR links for all candidate IF IDs in one scoped SuiteQL query, then all linked IR→IF owners in a second scoped query. The verified operational/receipt REST line reads, TO/IF native line mapping, and quantity calculations are unchanged. Incomplete batch results fall back to the original per-IF reads; a bad individual IF remains excluded. The picker batch is request-local; selecting an IF and posting a receipt still fetch fresh native links.

The batch removes repeated native link and owner round trips, **not** the per-IF REST reads, so a further decrease in the roughly one-minute wait must be measured, not assumed. Rerun the same STR and compare the total verification time and counts (24 candidates, 7 pending, 0 skipped previously). If it remains slow, profile the remaining REST and source/metadata stages before redesigning discovery; do not rely on accounting-line aggregates or the custom fully-received checkbox as a substitute for verified operational quantities.

### Request-scoped source metadata batch

After the link batch, the same STR still took 47,315 ms (24 candidates, 7 pending, 0 skipped). The per-IF source/metadata stage was typically 1–2 seconds. Picker discovery now reads the native IF→TO receiving-line mappings for the candidate IDs in one scoped query, resolves the TO reference once, and fetches metadata only for the distinct mapped receiving-line IDs in one query. Each IF still compares its expanded REST lines to exactly one native mapping and exactly one matching TO metadata row. Shared metadata rows are copied before fulfillment-scoped shipped/received quantities are applied, preventing one IF from changing another's balance. Incomplete or invalid batch reads fall back to the existing per-IF read; only the picker shares this request-local snapshot. Selection and posting continue to re-read independently.

The combined mapped line set must fit a complete SuiteQL page; larger or unverifiable batches fall back rather than assuming missing rows are closed. A regression test uses two IFs sharing a TO receiving line, checks one batch read and distinct eligibility, and tests incomplete-batch fallback. Measure this change against STR 255540 and compare the 7 pending / 0 skipped result. Per-IF REST and receipt line verification remain and still contribute to load time.
