# NetSuite → WMS bulk fulfillment callback

## Scope

WMS exposes a callback endpoint for NetSuite to report that a Bulk Fulfillment (BF) request finished. NetSuite owns IF creation and callback delivery/retries. WMS does not create IFs in response to notifications.

Per the final requested scope, this implementation uses **process-local memory only**. There are no database tables, migrations, files, or durable queues for this feature. The database changes initially scaffolded during development were removed and were never applied.

## User workflow

1. An authenticated user submits at least two unique RTS records using Bulk Fulfill.
2. WMS registers the submitting user's identity in memory and associates the returned NetSuite BF ID with that user.
3. Upon acceptance, WMS displays **Fulfillment processing** for **3 seconds**. This does not mean IF creation has completed.
4. NetSuite finishes its server-side processing and POSTs the callback below.
5. WMS accepts the event into memory and invalidates Supplier Return and Packing grid caches on the receiving instance. Existing displayed transaction grids load fresh data on their next refresh; the callback does not directly edit displayed rows or local fulfillment records.
6. A notification component in the protected layout checks the **local in-memory notification store** every 5 seconds. It does not poll NetSuite.
7. The submitting user sees a sticky Done or Failed toast with the BF reference and optional NetSuite message. It stays until **Dismiss** is clicked. It survives internal navigation because it lives in the layout.
8. Dismissal is user-scoped. A later, higher-sequence status event for the same BF can produce a new notification.

Done is treated as the NS-admin-confirmed signal that IFs are available. Failed messages may describe partial fulfillment. A populated `message` does not itself mean failure; `status` determines the outcome. Messages are rendered as text, not HTML.

## Endpoint and authentication

```http
POST https://<wms-host>/api/integrations/netsuite/bulk-fulfillment-events
Content-Type: application/json
X-NetSuite-Webhook-Key: <shared-secret>
```

Configure the WMS host using its secret configuration provider or this environment variable:

```text
NetSuiteWebhook__ApiKey
```

Use a securely generated secret of at least 32 characters and configure the same value in the NetSuite script's secure configuration. No real key is included in source or examples. The endpoint authenticates with this machine credential, not a WMS login cookie. Key comparison uses fixed-time comparison of hashes.

The deployment URL must be reachable from NetSuite over HTTPS. `localhost` is not reachable from NetSuite. If HTTPS terminates at a reverse proxy, configure ASP.NET Core forwarded headers for that trusted proxy so `Request.IsHttps` reflects the original HTTPS request. Do not enable untrusted forwarded headers globally.

## Request contract

```json
{
  "eventId": "27e74b95-47e9-4e2c-99f1-a50506f5c7d2",
  "bulkFulfillmentId": 301,
  "sequence": 1,
  "status": "Done",
  "message": "2 of 2 RTS fulfilled. Item Fulfillments are available."
}
```

| Field | Contract |
| --- | --- |
| `eventId` | Required nonempty UUID. Create once for a logical status event and reuse it on delivery retries. |
| `bulkFulfillmentId` | Required positive integer: the NetSuite BF internal ID, not an RTS or IF ID. |
| `sequence` | Required positive integer, strictly increasing per BF for each new status event. Reuse the same value on retries. |
| `status` | Required, case-sensitive `Done` or `Failed`. These are text labels, not status list IDs. |
| `message` | Optional plain text, at most 4,000 characters. Use the BF processing details if appropriate. |

Maximum request body size: 16 KiB. For non-ASCII messages, keep the encoded body within that limit too.

Example failure:

```json
{
  "eventId": "83a075ee-0eb5-468f-a234-80f9108caf6a",
  "bulkFulfillmentId": 301,
  "sequence": 2,
  "status": "Failed",
  "message": "NetSuite processing failed. Review the BF record; some IFs may already exist."
}
```

If an administrator retries the same BF and it later completes, send a **new UUID and higher sequence**. Delivery attempts of the same event must keep the entire payload identical. The sequence comes from NetSuite's processing version, not the HTTP retry count.

## Responses and retry behavior

| HTTP response | Meaning / sender behavior |
| --- | --- |
| `202 Accepted` | Accepted into current-process memory, or identical duplicate already received. Stop delivery retries for this event. This is not a durable-delivery acknowledgment. |
| `400 Bad Request` | Invalid fields or HTTP instead of HTTPS. Correct the contract/URL before retrying. |
| `401 Unauthorized` | Missing/incorrect machine key. Correct the credential. |
| `409 Conflict` | Event UUID or BF+sequence was reused with a different payload. Investigate; do not silently renumber an existing event. |
| `413 Payload Too Large` | Body exceeded the size limit. Shorten the message. |
| `503 Service Unavailable` | WMS key is not configured/too short, or the in-memory notification store is at capacity. Investigate configuration/capacity; retry after resolution. |
| Connection error / timeout / other `5xx` | Retry the identical notification with backoff. Never rerun fulfillment creation just because notification delivery failed. |

ASP.NET Core may return its standard validation/problem response body for malformed JSON. NetSuite should use the HTTP status, not parse a particular error string.

## Memory-only delivery boundaries

- User/BF routing, event deduplication, pending messages, and dismissal state are all lost when WMS restarts.
- A browser refresh may recover an unread message if the same WMS process is still running and the same user signs in. This is not durable persistence.
- The store is bounded at 10,000 submission registrations and 10,000 received events per process. A full store rejects new work rather than silently losing notifications.
- The endpoint can receive a fast callback before submission receipt association; it becomes visible when the BF reference is attached to its owner.
- A callback without a matching in-memory WMS submission has no user recipient and will not be broadcast to other users. This includes BF requests submitted before this feature was deployed or before a restart.
- Accepted submissions without a recoverable BF ID cannot be automatically associated with notifications; users must inspect NetSuite.
- This deployment model requires a single WMS instance, or routing that keeps submission and callback handling on the same process. Independent instances do not share memory or cache invalidations.
- NetSuite retries solve failed delivery, not loss after WMS already replied `202` or loss of ownership mappings during a restart.

## Code map

- Endpoint: `Web.BlazorServer/Controllers/NetSuiteBulkFulfillmentController.cs`
- Callback validation: `Application.UseCases/Commands/Transaction/SupplierReturn/ReceiveBulkFulfillmentEventCmd.cs`
- Process-local store: `Web.BlazorServer/Services/Implementation/InMemoryBulkFulfillmentTrackingRepository.cs`
- Sticky layout component: `Web.BlazorServer/Components/Shared/Others/BulkFulfillmentNotifications.razor`
- Submission toast: `Web.BlazorServer/Services/Implementation/ToastService.cs`
- Machine-readable contract: [netsuite-bulk-fulfillment-callback.openapi.yaml](netsuite-bulk-fulfillment-callback.openapi.yaml)

## Verification

- Web suite: 97 tests passed, including callback authentication, contract validation, duplicates/conflicts, out-of-order events, owner-only access, dismissal, memory loss on new store creation, a three-second submission toast, and safe rendering of sticky messages.
- Focused bulk fulfillment integration tests: 16 passed.
- Full integration suite: 855 passed, 1 failed in the unrelated `Purchase_order_list_query_excludes_custom_closed_status_but_allows_unset_status` assertion (expected `t.status IN ('B', 'E')`). No PO query or test was changed for this feature.
- No live NetSuite callback or IF creation was performed during verification. Configure the host/key and coordinate a test callback with the NS admin before deployment use.
