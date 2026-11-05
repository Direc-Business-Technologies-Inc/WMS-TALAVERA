# Receiving Item Receipt — Duplicate Line Investigation

## Summary

The colleague's screenshot appears to show the line grid on the **Create Item Receipt** page, rather than the Receiving **Purchase Orders** list. The affected transfer order was queried with the same `RECEIVING` transaction-line filter as the application, and the result contained repeated source line IDs. This confirms duplicate joined rows for that order; the specific join multiplicity was not conclusively isolated for every repeated row.

## Scope and UI path

The screenshot shows item/barcode-style values, item descriptions, warehouse/details, selection checkboxes, and quantity inputs. Those match the line grid in `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/ItemReceiptForm.razor` (lines 78–85 and 105–149), which renders `Data.Lines` and quantity entry controls.

The page loads source data in `Web.BlazorServer/Components/Pages/Transaction/Receiving/ItemReceiptCreatePage.razor.cs` (lines 47–59) by calling `GetItemReceiptSourceAsync`. The application query `Application.UseCases/Queries/Transaction/Receiving/GetItemReceiptSourceQry.cs` (lines 17–34) obtains the header and then the corresponding item-receipt lines. The NetSuite line query is in `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` (lines 735–777).

This is distinct from the PO list grid, which displays order-level columns such as reference number, dates, vendor, and status. Its query is also in `ReceivingIntegration.cs` (lines 165–215), but that is not the data source for the screenshot's quantity-entry rows.

## Potential duplicate mechanism

The item-receipt query selects the source transaction line ID as `LineNumber` (`ReceivingIntegration.cs:743`) and joins transaction lines to item, location, and UoM data. It also has two joins that may be one-to-many for a given item/location/UoM:

- Preferred-bin assignment subquery, joined by item and location (`ReceivingIntegration.cs:762`).
- Barcode-per-UoM records, joined by UoM and item (`ReceivingIntegration.cs:763`).

The query does not use `DISTINCT` or grouping before executing and returning the results (`ReceivingIntegration.cs:738–777`). If either join produces multiple matches for a source transaction line, the result set can contain repeated rows for that line. The UI binds the returned collection directly to `Data.Lines`, so repeated query results can appear as repeated grid rows.

The affected transfer order's query results included repeated source line IDs `153`, `156`, and `159`. Line `153` had two different barcode values; lines `156` and `159` each had two identical-looking projected rows. This confirms duplicate joined results for the order, but the results alone do not identify the underlying matching table for every duplicate. Repeated-looking rows can be caused by distinct underlying records not included in the selected columns.

The query was rerun with `tl.transactionlinetype = 'RECEIVING'`, matching the transfer-order filter in the application. Duplicate IDs remained, so the result applies to the transfer-order receipt query path and not only to unrelated transaction lines.

## Separate PO-list assessment

The Receiving Purchase Orders list query joins `transactionline` but selects order/header-level values and calls `.Distinct()` (`ReceivingIntegration.cs:176–204`). The selected location is `t.location`, not `tl.location`; therefore, differing transaction-line locations alone do not make otherwise identical selected rows survive `DISTINCT`. The earlier assertion that this makes duplicate PO-list rows possible was incorrect and is withdrawn.

## Implemented remediation

`ReceivingIntegration.GetItemReceiptLinesAsync` now aggregates the two auxiliary sources before joining them to transaction lines:

- Preferred-bin rows are grouped by item and location. A bin assignment is returned only if exactly one distinct preferred bin exists; ambiguous multiple-bin data returns `NULL` rather than selecting a potentially incorrect posting bin.
- Barcode-per-UoM rows are grouped by item and UoM, using a stable representative (`MIN(name)`) for the grid's single barcode display column. Barcode resolution continues to use its separate lookup path.

This makes each auxiliary join at most one row per join key and prevents those joins from multiplying valid source lines. It preserves distinct PO lines even when they share an item. A query-shape regression test was added at `Integration.NS.Tests/Implementations/Transactions/GetItemReceiptLinesQueryShapeTests.cs`.

## Verification and follow-up

For the affected PO, inspect the actual query results and group by `LineNumber`:

1. Run the updated query against the affected order and verify each source line ID occurs once.
2. Verify receipt display and posting behavior for an order whose item/UoM has multiple barcode records and whose item/location has multiple preferred-bin flags.
3. Confirm separate source lines for the same item remain separate rows.
4. Validate aggregate syntax and behavior against live NetSuite before deployment.

Use the source line identifier (`LineNumber`) as the identity for validation. Do not deduplicate by item code or barcode alone: a PO may validly contain separate lines for the same item, potentially with different locations, units, or quantities.

## Finding status

**Confirmed query-level duplicate-line result for transfer order `ISTR-THM00300000521`; remediation implemented but requires live NetSuite validation.** The PO-list duplicate claim is not supported by the reviewed query. The relevant defect was in the Create Item Receipt source-line query, where auxiliary joins could return more than one row for a source line.
