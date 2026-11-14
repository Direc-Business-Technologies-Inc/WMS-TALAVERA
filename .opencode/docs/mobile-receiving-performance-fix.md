# Mobile Receiving TO performance fix

Date: 2026-10-26

## Background

The investigation in `mobile-receiving-performance-findings.md` identified two
verified code-level risks in the mobile Transfer Order (TO) receiving list:

- The query joined every order line and applied a leading-wildcard match against
  NetSuite display names to enforce the employee's allowed locations. `DISTINCT`
  then removed duplicate orders created by the line join.
- The API did not request a bounded result page, so the mobile app fetched and
  rendered the whole endpoint response at once.

These risks are not proof of a production root cause; NetSuite runtime timing and
payload measurements are still required.

## Implemented changes

### Location access uses internal IDs

`Integration.NS/NSScripts/NS_TransferOrder_Get_PendingReceipt.sql` now checks
employee-location access through NetSuite's employee-location map table and matches
`map.maptwo` to `transactionline.location`. The correlated `EXISTS` preserves the
original rule—an order is visible when at least one line is in an allowed location—
without multiplying order rows. As a result, the outer `DISTINCT` and display-name
`LIKE` comparison are removed.

The map table name and columns follow the existing `LocationIntegration` query in
this repository. This SQL must still be confirmed in the configured NetSuite
sandbox because SuiteQL support and permissions are controlled by the account.

### Bounded list pages with explicit load-more

The Receiving TO API accepts `limit` and `offset` query parameters. The handler
clamps page size to 1–100 rows and offset to a nonnegative value, then passes both
to the existing NetSuite RESTlet paging support. Requests default to 50 rows.

The mobile page requests 50 orders initially and shows a **Load more** button when
the returned page is full. Each additional request appends the next page. Refresh
resets the offset and replaces the current data. Existing client-side search still
searches the rows loaded so far; load more when older orders are needed.

## Scope and boundaries

- This change pages only the mobile TO Receiving list. PO and Returns still have
  their existing list behavior.
- The query optimization changes query shape but is not an execution-plan or
  measured latency guarantee.
- The mobile response still uses the existing list result envelope; no total count
  or `hasMore` metadata was added. The UI infers further data from a full page, so
  an exact-multiple final page may show Load more once and receive an empty page.
- Stable paging depends on NetSuite's last-modified ordering. Changes to records
  between page requests can move rows across page boundaries; consider a snapshot
  cursor or deterministic secondary sort if field experience shows duplicates or
  omissions.

## Verification

Run the solution build and test suites for the API/application handler and mobile
page. In the NetSuite sandbox, compare old and new query results for representative
users, subsidiaries, and transfer-order IDs. Confirm both user-visible paging and
location authorization, and capture request duration, response row count, and
payload size before/after.

No live NetSuite writes are performed by these list changes. No schema migration is
required.
