# Analysis — STR packing vendor gate: review, findings, and hardening decisions

**Date:** 2026-10-22
**Type:** review session analysis / compaction
**Subject:** `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md`
**Outcome:** defect confirmed, plan finalized, specification written
**Spec:** `.opencode/specs/2026-10-22-str-packing-line-read-hardening/` (`requirements.md`, `design.md`, `tasks.md`)

## TL;DR

The 2026-10-21 vendor-gated packing change is safe to run **as shipped** — the save
path is untouched and the web packing flow remains an ungated fallback. But it sits
on top of a pre-existing read defect that matters at this operation's order sizes:

- A sample STR has **1,657** SHIPPING lines; the app receives at most **1,000**.
  Rows 1,001+ are silently discarded.
- The same read produces **1,661 joined rows for 1,657 distinct lines** — 4 duplicates.
- One document carries **95 distinct vendors**, so the gate's per-scan full read is
  repeated ~95 times to use ~17 rows each.

Two findings from the first review pass were later **retracted or downgraded** after
verification (see *Findings ledger*). The final plan is a paging fix, a structural
de-duplication, a call-scoped vendor predicate, a cheaper resolver, and two small
mobile UX fixes.

---

## How the session progressed

| Stage | Question | Result |
|---|---|---|
| 1 | Review the change; is it safe, will it work? | Safe; two issues ranked Critical |
| 2 | Re-review my own findings | 1 retracted, 1 downgraded, several narrowed |
| 3 | Is it usable now? | Yes, with an ops workaround |
| 4 | Performance at 1k-line average | Found the 1,000-row truncation as the dominant risk |
| 5 | Paging explanation + Phase 2 measurement plan | Paging cost model, SuiteQL runbook |
| 6 | Empirical data supplied | 1,657 lines, 1,661/1,657 fan-out, 95 vendors |
| 7 | Finalize harden/optimize/stabilize | Amendment D9 (vendor scope in SQL) |
| 8 | Write the spec | Three documents written at the path above |

---

## Findings ledger

The most useful artifact of this session: what survived scrutiny and what did not.
Severity here is the **final** disposition, not the first impression.

| # | Finding | Disposition | Why |
|---|---|---|---|
| 1 | 1,000-row truncation of the line read | **Critical — confirmed defect** | Measured 1,657 lines; `NetsuiteQuery` discards `hasMore` |
| 2 | Join fan-out producing duplicate lines | **High — confirmed defect** | Measured 1,661 joined rows vs 1,657 distinct |
| 3 | Resolver reads all lines twice per scan | **High — optimization + correctness** | 95 vendors per document; also hides vendors past row 1,000 |
| 4 | No feedback during vendor resolve | **Medium — real gap** | No busy state during the resolve round trip |
| 5 | Change Vendor discards unsaved scans | **Medium — narrowed** | Loss is real; the "bypasses an existing guard" claim was wrong |
| 6 | Resolver exceptions surface as raw 500 | **Low/Medium — real** | API has no exception middleware |
| 7 | Doc §4 save-path rationale is wrong for mobile | **Low — real, documentation** | Mobile posts only scanned lines |
| 8 | Stale test comment (barcode = internal id) | **Low — trivial** | Contradicts implementation and field test |
| 9 | Null-vendor documents dead-end on the handheld | **Downgraded to data question** | Vendor required since 2026-10-06; only pre-fix documents affected |
| 10 | Stale item load can paint vendor A's lines under vendor B | **RETRACTED** | `_resolvingVendor` spans the whole load, so resolves cannot interleave |
| 11 | `GoodSelectedLine` not cleared on vendor change | **Dropped** | Line sequence numbers are unique per TO; cannot cross vendors |
| 12 | `IN (...)` list unbounded | **Dropped** | One document's vendor set is tens, not thousands |

### Why #9 and #10 changed

**#9** — I first claimed "orders with no vendor were previously fulfillable", which
made it a regression. Verification showed the per-line vendor has been **required on
STR submit since 2026-10-06** and was never populated before that fix. The affected
population is therefore documents created in a two-week window that are *still* open —
a data question to measure, not a code path to build. Recommendation changed from
"add an ungated fallback" to "one query, then either a doc note or a backfill".

**#10** — I claimed a race where a slow load for vendor A lands after vendor B's.
Verification showed `ResolveVendorScan` awaits the item load *inside* the guarded
region (`TransferOrderItemView.razor.cs:536`, released at `:588-590`), so a second
vendor cannot begin resolving until the first load has applied. The invariant holds;
the fix I proposed would have defended an unreachable state.

---

## Empirical evidence gathered

All from the SuiteQL console on the production account, 2026-10-22.

| Measurement | Value | Meaning |
|---|---|---|
| SHIPPING lines on sample STR | **1,657** | Exceeds the 1,000-row page |
| Joined rows / distinct lines | **1,661 / 1,657** | 4 duplicate rows (fan-out confirmed) |
| Distinct vendors on that document | **95** | ~17 lines per vendor on average |

Query results along the way, including the failures that became design constraints:

- `GROUP BY vendor, BUILTIN.DF(vendor)` → **failed**. SuiteQL rejects `BUILTIN.DF` in `GROUP BY`.
- `GROUP BY vendor ... ORDER BY COUNT(*) DESC` → **failed**. SuiteQL rejects ordering by an aggregate expression.
- `GROUP BY vendor ... ORDER BY <alias>` → returned **95** rows.
- `SELECT DISTINCT vendor` (no aggregation) → returned **95** rows — the only form relied on by the final design.
- Join-fan-out comparison (`COUNT(*)` vs `COUNT(DISTINCT line id)`) → **1,661 / 1,657**.

---

## Verified code facts that shaped the plan

| Fact | Location |
|---|---|
| `hasMore` is discarded; only `items` returned | `Integration.NS/Services/NetSuiteApiClientService.cs:916-918` |
| `limit`/`offset` appended only when `limit > 0` | `NetSuiteApiClientService.cs:895-898` |
| Response DTO already models `hasMore` / `totalResults` | `NetsuiteTransactionApiDTO.cs:24-30` |
| `@key` splices a **quoted** literal; `{key}` splices a **validated integer list** | `Integration.NS/Helpers/SuiteQLText.cs:52`, `:69-89` |
| No exception middleware in the API | `Api.CoreWebAPI/Program.cs` |
| `<Loading>` **hides** children rather than overlaying them | `Mobile.MAUI/Components/Reusables/Loading.razor:17` |
| `ExecuteAppActionAsync` absorbs exceptions into `Result` | `Mobile.MAUI/Services/ActionFactoryService.cs:57-69` |
| `_resolvingVendor` set before the first await, held across the load | `TransferOrderItemView.razor.cs:583`, `:536`, `:588-590` |
| Save payload already deduped on `(line, IsBad, quantity)` | `NetSuiteApiClientService.cs:1698` |
| Unsaved-scan confirm exists on back navigation | `PageHeading.razor:4` → `Confirmable.razor.cs:15-17` |
| Receiving uses a **separate** handler and script | `Application.UseCases/Queries/Transaction/Receiving/...` |

---

## Final decisions

### Build

1. **Order + page the line read** — `ORDER BY tl.id`, loop 1,000-row pages in the handler, fail on any partial.
2. **Fix the fan-out structurally** — pre-aggregate the four bare `inventorybalance` joins with `SUM`.
3. **Apply the vendor scope in SQL** — `AND ({vendorId} = 0 OR …)`, `0` = unscoped.
4. **Replace the resolver's full read** — `SELECT DISTINCT` vendor ids + existing vendor-record name enrichment.
5. **Resolve indicator** on the gate card (spinner, not progress bar).
6. **Change Vendor: Save / Discard / Cancel** — switch only on a successful save.
7. **Actionable no-vendor message**; `try/catch` in the resolver.
8. **Documentation corrections** (save-path rationale, barcode = name, busy-flag invariant).

### Do not build

- Ungated fallback for null-vendor documents.
- Merged multi-vendor save payloads (`mixedBinFlags` makes them a blast-radius multiplier).
- In-memory scan retention (per-vendor bursts average ~17 lines).
- Determinate progress bar (no truthful increment without paging; dropped).
- `@vendorId` on the shared script (would splice a quoted string against a numeric field).
- A second vendor-scoped script (drift risk; the sentinel predicate avoids it).

### Deferred

- Timing records and text-file export of request/response data.
- Device verification (no device available during the session).

---

## Stability constraints discovered the hard way

Recorded so implementation does not rediscover them:

1. `BUILTIN.DF` cannot appear in `GROUP BY` on this account.
2. `ORDER BY <aggregate expression>` is rejected; order by an alias instead.
3. `.sql` files are whitespace-collapsed before sending — **no `--` or `/* */` comments** in them.
4. `{key}` is the only raw-splice form allowed, and only for comma-separated integers.
5. `SELECT DISTINCT` over a function is unproven here — the candidate query returns **ids only**.
6. `AS` an alias in the projection is required for clean deserialization into the DTO.

---

## Carried risks and open items

1. **T5/T6 modify the script the save path also reads.** Mitigated by the `0` sentinel and Checkpoint B (unscoped read must still return 1,657), but it is the one change with real blast radius.
2. **No device verification.** All reasoning is from code and SuiteQL; handheld behaviour is unproven.
3. **Two unknown data points:** the largest per-vendor line count, and whether a `NULL` vendor was among the 95. Neither changes the design; both affect performance expectations.
4. **Sanity check outstanding:** confirm the printed STR really carries ~95 vendor sections, rather than item-master defaults having been written into the line vendor.
5. **Documentation drift in the repo:** `.claude/` does not exist and `CLAUDE.md` describes an unrelated LSMS/SAP solution. The spec was written from verified code instead.
6. **Prior decision reconciled, not overridden:** `2026-10-16-to-partial-fulfillment-if-fix/plan.md:77` said the script must not filter rows in SQL; the new predicate is call-scoped with a default that reproduces the old row set.

---

## Process notes

Two lessons from this session, kept because they affect how the plan should be trusted:

1. **Severity was assigned before verification.** The two "Critical" findings from the
   first pass did not survive contact with the code — one was unreachable, the other
   was a data question. The re-review pass was what made the plan usable. Treat the
   *Findings ledger* as the authoritative record, not the first review.
2. **The most important finding was not in the change at all.** The 1,000-row
   truncation is pre-existing; it only became urgent once the gate started depending
   on the complete line set. Reviews should ask "what does this change now depend on?"
   rather than only "what did it touch?"

---

## Where to go next

1. Run the two remaining SuiteQL checks (largest per-vendor count; `NULL` bucket).
2. Confirm the ~95-section print assumption with the print owner.
3. Implement per `.opencode/specs/2026-10-22-str-packing-line-read-hardening/tasks.md`,
   branch `claude/fix/str-packing-line-read`, stopping at Checkpoint A and Checkpoint B.
4. Capture before/after timings via Postman for the change document.
5. Schedule a device test on a multi-vendor, 1,000+ line STR before removing the
   "unproven on device" caveat from the change doc.
