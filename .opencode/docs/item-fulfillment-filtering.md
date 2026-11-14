# Item Fulfillment Filtering

Requirement: hide already-Fulfilled IFs from **Trip Ticket creation**, and already-received IFs from **STR Receiving (Web)**.

Terminology:

- **IF** = Item Fulfillment.
- **Fulfilled IF** = IF with **Shipped** status `'C'`.
- IF status codes: `A` = Picked, `B` = Packed, `C` = Shipped.

## STR Receiving — native relationship filter

Already-received IFs were still listed in the IF picker.

**WMS query** — `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`

NetSuite exposes a standard IF-to-IR relationship in `nexttransactionlinelink`:

```sql
previousdoc = Item Fulfillment internal id
nextdoc     = Item Receipt internal id
previoustype = 'ItemShip'
nexttype     = 'ItemRcpt'
linktype     = 'TOrdCost'
```

`GetSTRItemFulfillments` aggregates material quantities on each shipped IF and
the material quantities on all natively linked IRs. It offers only IFs where
linked received quantity is less than fulfilled quantity. No custom completion
field and no RESTlet 1853 change are required.

Do not substitute the similarly named `nexttransactionlink` header table. In
this account, SuiteQL requests against that table return `500 UNEXPECTED_ERROR`
even for one fulfillment and without joins.

The native header relationship has `previousline = 0` and does not allocate
quantities by line. For receipt entry and server validation, WMS combines that
header relationship with the existing TO-to-IR links in
`previoustransactionlinelink`; it never matches lines by item text.

Affects both entry points that share `ItemFulfillmentsDialog`:

- `TransferOrderDetails.razor:115`
- `ReturnsDetails.razor:95`

## Trip Ticket creation — no change needed

`Integration.NS/Implementations/Transactions/TripTicketIntegration.cs:93`

`GetItemfulfillments` already filters `t.status = 'B'` (Packed only), so Shipped IFs (`'C'`) were never selectable. No code change.

## Remaining operational work

- **Status drift** — `NetSuiteApiClientService.cs:1969` `ChangeShipStatus` is best-effort: it PATCHes to Shipped but never verifies success or rolls back. A failed PATCH leaves the IF at `'B'`, and it reappears in the TT picker despite being fulfilled in the UI.
- **Native-link validation** — the `TOrdCost` relationship was verified against IF `118077` and IR `123541`. The deployment gate still includes partial, multiple-receipt, and multiple-IF sandbox cases.
- **No trip-ticket anti-join** — `GetItemfulfillments` does not exclude IFs already on a live trip ticket (`customrecord_dbti_trip_ticket_if` with `custrecord_dbti_ttf_cancelled_trip = 'F'`).
- **OR-collapse bug** — `TripTicketIntegration.cs:152-164` ANDs `Any(= 'F', IS NULL)` with `Equal(..., 'F')`, collapsing the OR and silently dropping IFs with a `NULL` flag. Correct pattern is at `TripTicketIntegration.cs:96`.
