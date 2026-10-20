# Mobile Inventory Worksheet — `adjustqtyby` Diagnostic

**Status:** Diagnostic shipped (API-only). Temporary marker — remove after sign-off.
**Owner path:** `Integration.NS/Services/NetSuiteApiClientService.cs` → `PostInventoryWorksheet`
**Related:** [mobile-netsuite-4xx-catalog.md](mobile-netsuite-4xx-catalog.md) (section 5), [WMS_PROCESS_FLOW.md](WMS_PROCESS_FLOW.md) (5.4.C)

---

## 1. Purpose

Field reports show mobile Inventory Worksheet saves failing with:

```
Action failed: Error while posting Inventory Worksheet. NetSuite 400 Bad Request
{"error":{"code":"INVALID_FLD_VALUE","message":"...You have entered an Invalid Field
 Value for the field adjustqtyby contained more than the maximum number (15) of
 characters allowed."}}
```

Two things were unknown:

1. **Whose value is it?** The WMS payload has no `adjustqtyby` field. The field is
   written by NetSuite Restlet `script=1878`, which is **not in this repository**.
2. **What is the value?** Testing happens on the client's device, so neither the
   device nor the API server logs were reachable at report time.

This diagnostic answers both from a **single operator screenshot**, with no log access.

---

## 2. What was added

A `[diag …]` suffix is appended to the error message the mobile toast already renders.
The same pattern is used by the PO receipt and TO fulfillment flows
(`NetSuiteApiClientService.cs:1000`, `:1022`, `:1632`, `:1676`).

```csharp
public async Task<bool> PostInventoryWorksheet(List<InventoryWorksheetLineDTO> Data, int Location, int Subsidiary)
{
    string url = InventoryWorksheetRestletUrl;
    PostInventoryWorksheetPayload? payloadGood = null;

    try
    {
        payloadGood = PostInventoryWorksheetPayload.PostInventoryWorksheet(Data, Location, Subsidiary);
        var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);
        await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
    }
    catch (Exception ex)
    {
        throw new Exception($"Error while posting Inventory Worksheet. {ex.Message} {BuildWorksheetDiag(payloadGood, Data)}", ex);
    }

    return true;
}
```

Marker format:

```
[diag lines={n} scanned={n} item={invtid} qty={value} len={n} maxLen={n} build={stamp}]
```

### Why the payload is moved outside the `try`

The diag must inspect the payload **that was actually sent**. Declaring it before the
`try` lets the `catch` read it even when serialization or the HTTP call throws.
`BuildWorksheetDiag` is total — it never throws, and returns
`[diag unavailable build=…]` as a last resort.

### Why length is measured on the serialized value

`decimal` **preserves scale** through `System.Text.Json`, so the scale is what reaches
the wire. Measuring `JsonSerializer.Serialize(value).Length` reports the exact
character count NetSuite sees; `ToString()` can differ and would under-report.

---

## 3. Field dictionary

| Field      | Meaning |
|------------|---------|
| `lines`    | Number of worksheet lines posted (one per item, grouped by `NetsuiteMaterialInternalId`). |
| `scanned`  | Mobile lines that had Good or Bad quantity > 0. |
| `item`     | `invtid` of the **longest-numbered** line — the prime suspect. |
| `qty`      | That line's numeric value, invariant-culture, exactly as the candidate offender. |
| `len`      | Serialized character length of that value, including the decimal point. |
| `maxLen`   | Longest serialized length found across `newqty` **and** every `inventoryDetail[].quantity`. |
| `build`    | API assembly informational version, for deploy verification. |

`qty`/`len` consider both `newqty` (the summed line quantity) and each
`inventoryDetail[].quantity`, because Restlet 1878 may derive `adjustqtyby` from
either. `maxLen == len` is guaranteed by construction.

---

## 4. How to read it

NetSuite's limit is **15 characters**. Compare `maxLen`:

| Observation | Conclusion | Next action |
|-------------|------------|-------------|
| `maxLen > 15` | **Ours.** The payload builder emitted an over-long decimal. | Normalize to 5 dp in `PostInventoryWorksheetPayloadDTO.cs:97` using `RoundOfNearestHundredThousands`, then add a bound. |
| `maxLen <= 15` | **NetSuite's.** We sent a legal value; Restlet 1878 created the over-long one (summing, UoM conversion, or a rate multiply). | Escalate to the NetSuite team with `qty`, `len`, and the payload from the server log. |
| `[diag unavailable …]` | Payload construction itself failed, before anything was sent. | Different defect — the failure is not the NetSuite rejection. |

### Sample outputs

Healthy payload:

```
[diag lines=2 scanned=2 item=47776 qty=12 len=2 maxLen=2 build=1.0.0+5598a5da]
```

Over-long decimal — ours, from unrounded quantity arithmetic or free-text manual entry:

```
[diag lines=1 scanned=1 item=47776 qty=3.3333333333333333333333333333 len=30 maxLen=30 build=1.0.0+5598a5da]
```

Small number, NetSuite still rejects — blame the Restlet:

```
[diag lines=3 scanned=3 item=50123 qty=8 len=1 maxLen=1 build=1.0.0+5598a5da]
```

---

## 5. Deployment

- **API redeploy only. No APK rebuild, no OTA.** The marker rides in `ex.Message`;
  `PostInventoryWorksheetCmd` wraps it into `ApiResult.ServerError`, and the installed
  mobile build already renders that string in its toast. The client retests with the
  app they already have.
- No configuration change. `Logging:LogLevel:Default` is `Information`
  (`Api.CoreWebAPI/appsettings.json:4`), which includes warnings, so the **pre-existing**
  full-payload log in `LogNonSuccessResponse` (`NetSuiteApiClientService.cs:869-884`,
  called from `:478`) is also available if the API host's stdout is captured.

---

## 6. What this does not prove

The marker proves **what we sent**, not what NetSuite received. If `maxLen <= 15`, the
over-long value was produced server-side by Restlet 1878, and only the NetSuite team can
confirm the transformation. The marker narrows the question; it does not close it.

It also does not capture `location`, `subsidiary`, or bin context. Those are visible in
the full request payload logged at `:478` if server logs are reachable.

---

## 7. Removal checklist (after sign-off)

1. Delete `BuildWorksheetDiag`.
2. Remove `PostInventoryWorksheetPayload? payloadGood = null;` and restore
   `var payloadGood = …` inside the `try`.
3. Restore the throw to `$"Error while posting Inventory Worksheet. {ex.Message}"`.
4. Remove the `[diag]` entry from the 4xx catalog's open-items list.

This follows the same cleanup debt already tracked for the PO and TO `[diag …]`
markers.
