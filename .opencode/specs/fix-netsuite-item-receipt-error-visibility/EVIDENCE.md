# Evidence — NetSuite Item Receipt Error Visibility

**Spec name:** `fix-netsuite-item-receipt-error-visibility`
**Collected:** 2026-10-06
**Method:** static analysis of the WMS_TALAVERA repository. No runtime access, no NetSuite
account, no database access.

## Reported Symptom

- Toast: `Create Item Receipt failed: Error posting items: Exception of type 'System.Exception' was thrown.`
- Page: `https://192.168.5.27:9767/transactions/purchasing/receiving/create-item-receipt?ref=iSTR-THM00100000027&itemFulfillment=IF-THM001000...`
- The captured URL is truncated, so the presence of an `ifid` parameter could not be
  confirmed.

## Confirmed Defects

| ID | Location | Evidence |
|----|----------|----------|
| 1.1 | `Integration.NS/Services/NetSuiteApiClientService.cs:469` | `throw new Exception(errorDetail)` passes a possibly-null value; the status-code message is commented out at `:468`. |
| 1.2 | `Integration.NS/Services/NetSuiteApiClientService.cs:472-518` | `GetNetSuiteErrorDetail` handles only `o:errorDetails[].detail` and `error` as a JSON **string** (`ValueKind == JsonValueKind.String` at `:512`). The RESTlet object shape returns null. |
| 1.3 | `Integration.NS` (whole project) | No `ILogger` usage anywhere in the project; the only logging convention in the solution appears in `Application.UseCases/Commands/Transaction/Packing/NS/TransferOrder/PostTransferOrderIFCmd.cs:16`. |
| 1.4 | `Application.DataTransferObjects/Transactions/Receiving/ItemReceiptDTO.cs:68` | `QuantityOpen` evaluates `QuantityPlanned - QuantityOpen`. |

## Supporting Trace

| Step | Location | Behavior |
|------|----------|----------|
| Toast prefix | `Web.BlazorServer/Services/Implementation/AppActionFactory.cs:53` | Formats `{ActionName} failed: {ex.Message}`. |
| Error rewrap | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:612` | Adds `Error posting items: {ex.Message}`; flattens to message only. |
| Dead filter | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:607` | `Empty response from NetSuite API` filter is unreachable — the OAuth1 path throws rather than returning an empty body. |
| Restlet endpoint | `Integration.NS/Services/NetSuiteApiClientService.cs:55` | `...restlet.nl?script=1853&deploy=1`. |
| Fulfillment id fallback | `Web.BlazorServer/Components/Pages/Transaction/Receiving/ItemReceiptCreatePage.razor.cs:75` | `FormData.ItemFulfillmentId = IfId ?? -1`. |

## Hypotheses (not confirmed)

### Bug Category 3 — orderLine divergence

| Path | Location | Value sent |
|------|----------|------------|
| Web | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:448` (subquery `:425-439`) | Correlated 4-table subquery: `previoustransactionlinelink` -> `transferorderitemlineid`, filtered `ttl.transactionlinetype = 'RECEIVING'` inside the TO transaction. |
| Mobile / siblings | `Integration.NS/NSScripts/NS_TransferOrder_Get_Items.sql:19` | `tl.id AS LineSequenceNumber` — the source document's own line id. |

Corroboration: every sibling script aliases `tl.id AS LineSequenceNumber`
(`NS_PurchaseOrder_Get_Items.sql:14`, `NS_TO_Get_Itemfulfillment_Items.sql:19`,
`NS_InventoryCounting_Get_Items.sql:9`, `NS_TO_x_Packing_Get_Items.sql:19`,
`NS_TO_x_Return_x_Packing_Get_Items.sql:19`, `NS_TransferOrder_x_Return_Get_Items.sql:19`,
`NS_VendorReturnAuthorization_Get_Items.sql:15`).

Risk: the web subquery is a `LEFT JOIN` / scalar subquery that can return no row, yielding
`LineNumber = 0` into the non-nullable `int` at `ItemReceiptDTO.cs:45`, which serializes as
`orderLine: 0`.

### Bug Category 4 — binNumber divergence

| Path | Location | Value sent |
|------|----------|------------|
| Web TO receipt | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:876` | `binNumber = y.Bin?.BinNumber` (string) |
| Web Returns receipt | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:840` | `binNumber = y.Bin?.BinNumber` (string) |
| Web PO receipt | `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` (CreatePOJson) | `Bin?.Id` (int) |
| Mobile TO receipt | `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/TransferOrderIRRestletPayloadDTO.cs` | field omitted entirely |

`BinNumber` is a `string` and `Id` is an `int`
(`Application.DataTransferObjects/Others/LocationBinDTO.cs:11-12`,
`Web.BlazorServer/ViewModels/Others/LocationBinVM.cs:5-6`).

## Environment Limitations

- **The RESTlet is not in this repository.** `glob **/*.js` returned only frontend assets
  (`Web.BlazorServer/wwwroot/js/custom-scripts/*`, `Mobile.MAUI/wwwroot/js/*`). The
  validation contract of script 1853 cannot be inspected statically, which is the reason
  Stage 3 is gated on runtime evidence.
- **No test projects exist.** `glob **/*.csproj` returned 12 production projects and no
  test project, so no automated regression suite is available.
- **No runtime or database access** was available; all findings are static.

## Recommended Next Evidence Step

NetSuite **Script Execution Log** filtered to Script ID `1853`, Deployment `1`. The log
records the failure and its exception detail for every restlet invocation, including the
failed receipt in question. This yields the discriminating signal without any code change.
