# Item Fulfillment Filtering

Requirement: hide already-Fulfilled IFs from **Trip Ticket creation**, and already-received IFs from **STR Receiving (Web)**.

Terminology:

- **IF** = Item Fulfillment.
- **Fulfilled IF** = IF with **Shipped** status `'C'`.
- IF status codes: `A` = Picked, `B` = Packed, `C` = Shipped.

## STR Receiving — fixed

Already-received IFs were still listed in the IF picker.

**Change** — `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:1136`

`GetSTRItemFulfillments` now excludes IFs flagged as fully received:

```sql
AND (t.custbody_dbti_fully_received = 'F' OR t.custbody_dbti_fully_received IS NULL)
```

`IS NULL` is required because the flag is a checkbox — unset reads as `NULL`, not `'F'`.

Affects both entry points that share `ItemFulfillmentsDialog`:

- `TransferOrderDetails.razor:115`
- `ReturnsDetails.razor:95`

## Trip Ticket creation — no change needed

`Integration.NS/Implementations/Transactions/TripTicketIntegration.cs:93`

`GetItemfulfillments` already filters `t.status = 'B'` (Packed only), so Shipped IFs (`'C'`) were never selectable. No code change.

## Known gaps (not addressed)

- **Status drift** — `NetSuiteApiClientService.cs:1969` `ChangeShipStatus` is best-effort: it PATCHes to Shipped but never verifies success or rolls back. A failed PATCH leaves the IF at `'B'`, and it reappears in the TT picker despite being fulfilled in the UI.
- **No trip-ticket anti-join** — `GetItemfulfillments` does not exclude IFs already on a live trip ticket (`customrecord_dbti_trip_ticket_if` with `custrecord_dbti_ttf_cancelled_trip = 'F'`).
- **OR-collapse bug** — `TripTicketIntegration.cs:152-164` ANDs `Any(= 'F', IS NULL)` with `Equal(..., 'F')`, collapsing the OR and silently dropping IFs with a `NULL` flag. Correct pattern is at `TripTicketIntegration.cs:96`.
