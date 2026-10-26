# Mobile Receiving Performance Findings

**Date:** 2026-10-24  
**Status:** Investigation complete; runtime profiling still required  
**Scope:** `Mobile.MAUI` Receiving, related mobile list pages, and their NetSuite-backed read paths

## Executive Summary

The Receiving TO tab performs one NetSuite-backed list request when the tab is rendered. The request uses a query with a potentially expensive shape: it joins transaction lines, applies a display-name-based location-access predicate with a leading-wildcard `LIKE`, removes duplicates with `DISTINCT`, and sorts by last-modified date.

This is the strongest code-level performance candidate for the reported TO loading delay. It is **not yet a confirmed runtime root cause** because the repository does not contain production timings or a NetSuite execution plan for the request.

The Receiving page uses Radzen server-rendered tabs. Only the selected tab content is rendered, so the TO, PO, and Returns list requests are not expected to start simultaneously merely because `/receiving` is opened.

## Verified Request Path

The TO request path is:

1. `Mobile.MAUI/Components/Pages/Receiving/ReceivingView.razor:7-17` defines PO, TO, and Returns using `TabRenderMode.Server`.
2. `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderView.razor.cs:53-78` loads the TO list on the component's first render.
3. The mobile component posts to `/Receiving/TransferOrder/PendingReceipt` at line 38.
4. `Api.CoreWebAPI/Controllers/Receiving/TransferOrderController.cs:19-26` dispatches the request to the receiving query handler.
5. `Application.UseCases/Queries/Transaction/Receiving/NS/TransferOrder/GetTransferOrdersQry.cs:19-26` calls `NetsuiteQuery<OrdersDTO>` with `NS_TransferOrder_Get_PendingReceipt`.
6. `Integration.NS/NSScripts/NS_TransferOrder_Get_PendingReceipt.sql` is loaded and sent to NetSuite by `NetSuiteApiClientService.NetsuiteQueryWithMetadata`.

The repository also contains a separate `GetTransferOrdersQry` for web data grids. That is not the handler used by the mobile Receiving TO endpoint.

## Findings

### F1. TO query has a potentially expensive access predicate

**Evidence:** `Integration.NS/NSScripts/NS_TransferOrder_Get_PendingReceipt.sql:11-13`

The query joins every candidate transaction to `transactionline`, then joins `employee` using this condition:

```sql
UPPER(TRIM(BUILTIN.DF(emp.custentity_dbti_wms_user_location_access)))
LIKE '%' || UPPER(TRIM(BUILTIN.DF(tl.location))) || '%'
```

This performs several transformations and uses a leading wildcard. It compares display values rather than location IDs. The query then uses `DISTINCT` because the line join can produce multiple rows for one order.

**Assessment:** High-priority query to measure and investigate. The code proves the query shape, but not its actual execution cost.

### F2. TO list has no application-controlled page size

**Evidence:** `GetTransferOrdersQry.cs:25`; `NetSuiteApiClientService.cs:897-929`

The mobile handler calls `NetsuiteQuery` without a `limit` or `offset`. The client defaults `limit` to `0`, and therefore does not add paging parameters to the NetSuite URL.

The mobile component receives the result into an in-memory list and renders that list through `RadzenDataList`:

- `TransferOrderView.razor.cs:43-44`
- `TransferOrderView.razor:35-61`

**Assessment:** Confirmed absence of application-controlled paging. This can increase response size, deserialization cost, and UI rendering work as the result set grows. It does not prove that NetSuite returns an unlimited result set; the effective response behavior is controlled by the NetSuite endpoint.

This is also a potential completeness concern if the number of matching records exceeds the endpoint's default response size.

### F3. Receiving tabs are not all fetched at page startup

**Evidence:** `ReceivingView.razor:7-17`; Radzen Tabs server-render documentation

Receiving configures `TabRenderMode.Server`. Radzen documents server rendering as rendering only the selected tab content. Therefore, opening the Receiving route should not, by itself, initiate all three list requests.

**Assessment:** This rules out simultaneous PO + TO + Returns loading as the default explanation for the TO delay. It does not rule out duplicate requests caused by refresh actions, navigation, or component lifecycle behavior during a specific session.

### F4. PO and Returns share the same access-filter risk

**Evidence:**

- `Integration.NS/NSScripts/NS_PurchaseOrder_Get_PendingReceipt.sql:11-18`
- `Integration.NS/NSScripts/NS_TransferOrder_x_Return_Get_PendingReceipt.sql:9-14`

Both list queries join transaction lines and use the same transformed, leading-wildcard location-access comparison. The PO query also joins `entity`; the Returns query uses `DISTINCT`.

**Assessment:** These are valid comparison candidates for profiling. Their source shape suggests a shared optimization opportunity, but no conclusion should be made about which page is slowest without timings.

### F5. NetSuite request queueing and retry can add delay

**Evidence:**

- `Integration.NS/Services/NetSuiteApiClientService.cs:124-132`
- `Integration.NS/Services/NetSuiteApiClientService.cs:398-417`
- `Integration.NS/Resilience/NetSuitePipelines.cs:20-22,60-70`

The client defaults to two concurrent in-flight NetSuite requests per process. Requests wait on a shared semaphore. Interactive reads can be retried once for a transient failure, with a default delay of approximately five seconds.

**Assessment:** Confirmed possible contributors, not evidence that the reported TO request queued or retried. Timing and logs are required to establish this for a slow session.

## Other Mobile Pages Reviewed

### Inventory Worksheet

**Evidence:**

- `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor.cs:233-259`
- `Application.UseCases/Queries/Transaction/InventoryCounting/NS/GetInventoryItemsQry.cs:15-20`
- `Integration.NS/NSScripts/NS_Get_InventoryItems.sql:1-9`

The worksheet loads inventory items on first render. The query selects all inventory parts and has no location or search filter. This is a confirmed broad fetch and should be profiled as the item catalog grows.

### Trip Ticket Details

**Evidence:** `Mobile.MAUI/Components/Pages/TripTicket/TripTicketDetailsView.razor.cs:154-175`

Six lookup actions are awaited sequentially: subsidiaries, destinations, drivers, helpers, truck plates, and origin locations.

This guarantees that their durations are additive before the dialog finishes loading. It is a confirmed sequential-fetch pattern, but not proof that it is currently a user-visible bottleneck.

### Packing Lists

Packing list scripts use grouped open-line subqueries before joining orders:

- `NS_TO_x_Packing_Get_PendingFulfillment.sql:11-17`
- `NS_TO_x_Return_x_Packing_Get_PendingFulfillment.sql:11-17`
- `NS_VendorReturnAuthorization_Get_PendingReturn.sql:15-20`

This is a query-shape risk worth measuring. The source alone does not establish that these pages are slow.

### Detail Pages

Receiving detail pages make separate requests after an order is selected. For example, the TO item page posts to `/Receiving/TransferOrder/Items` from `TransferOrderItemView.razor.cs:54-61`. These detail queries should not be attributed to the initial TO list delay unless the reported delay occurs after selecting an order.

## Findings Not Classified as Fetch Bottlenecks

- TO search performs local filtering after data has already loaded. Date formatting occurs per row while searching in `TransferOrderView.razor.cs:109-145`; this may affect search responsiveness, not initial network fetch time.
- The `IntersectionObserver.js` module is used for UI behavior and does not fetch list data.
- The existence of `DISTINCT`, grouped subqueries, or joins is not sufficient by itself to prove a production performance problem. NetSuite execution time and result cardinality must be measured.

## Recommended Verification Instrumentation

Capture these timestamps and values for a slow TO load:

1. Mobile request start and end.
2. API handler start and end.
3. Time waiting for the NetSuite request semaphore.
4. NetSuite HTTP request start and end.
5. Whether a retry occurred and why.
6. Response row count and response payload size.
7. Mobile deserialization and first meaningful render completion.

The measurements distinguish the main possible causes:

| Observation | Likely cause |
|---|---|
| NetSuite HTTP duration dominates | Query execution or NetSuite service latency |
| Semaphore wait dominates | Request concurrency/queueing |
| A retry adds about five seconds | Transient NetSuite failure |
| Network/API is fast but render is slow | Payload size or mobile rendering |
| Only search is slow after loading | Local filtering/date-formatting work |

## Recommended Investigation Order

1. Measure the current TO request with a real slow-user example.
2. Compare current TO SQL against a candidate access-filter rewrite using the same user, subsidiary, and expected order IDs.
3. Add a bounded server-side paging contract for Receiving lists, including a search or load-more path for older orders.
4. Profile PO and Returns using the same measurement points because they share the access-filter pattern.
5. Profile the Inventory Worksheet and Trip Ticket Details separately; do not assume they share the TO root cause.

## Final Conclusion

The repository supports this precise conclusion:

> The mobile Receiving TO tab sends a NetSuite list query with a potentially expensive line-level display-name access filter and no application-controlled page size. This is the strongest code-level candidate for the reported loading delay, but the root cause remains unconfirmed until request, NetSuite, queueing, retry, payload, and render timings are captured.

No SQL or application code was changed as part of this investigation.
