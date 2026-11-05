---
Date: 2026-10-09
Status: Confirmed
Mode: FEATURE
Scope: Stock Transfer Request — add "Requested By" header field (TO, ITO, Return) sourced from NetSuite employees, persisted to the standard `employee` field, and surfaced as a new list-grid column
Related: `requirements.md`, `design.md` (siblings)
---

# Tasks Document

**Goal:** Add a required, draft-editable NetSuite employee dropdown
("Requested By") to all three STR types, persisted to the standard `employee`
field and surfaced as a new list-grid column.

**Architecture:** VM → handler `Adapt` → MediatR command (unchanged) →
`Integration.NS` SuiteQL read + REST payload. The read path returns **id and
name**; the payload emits `employee: { id }` and omits the key when null.

## Global Constraints

1. `CreateSTRPayload`
   (`Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:356`)
   is shared by create **and** update — one edit covers both. Do not fork it
   (P-9).
2. Alias `e` is bound to `custbody_dbti_prepared_by` (line 183). The new alias
   **must** be `re` bound to `t.employee`. Do not reuse `e` (P-3).
3. Serializer is `CamelCase` + `WhenWritingNull` (lines 349-354). Key `employee`
   stays lowercase; null must **omit**, not send `null` (P-2).
4. Prepared By behavior is frozen: set from claim on create
   (`StockTransferRequestHandler.cs:78-81`), never set on update (lines 88-96)
   (P-10, Req 5.1/5.2).
5. Do **not** change `IsEditable` in either location
   (`StockTransferRequestIntegration.cs:207`,
   `StockTransferRequestDataGridNSDTO.cs:28`) — Req 4.
6. Do **not** touch the `ExecuteSuiteQLQuery` vs `ExecuteWithPaging`
   inconsistency at lines 103 / 145 — Req 4.
7. `Select` tuple order is `(expression, alias)` — verified at
   `EmployeeIntegration.cs:17-24`.
8. Work on a `claude/feat/` branch per `CLAUDE.md:33`.

---

## Phase 0 — Pre-flight

### T-0.1 — NetSuite-side dependency audit
**RESOLVED.** Confirmed with the NS admin: no approval workflow and no
SuiteScript reads or writes the standard `employee` field on `transferorder` /
`intercompanytransferorder`. Writing `employee` has no routing or scripting side
effects.
_Requirements: 3.5_

### T-0.2 — Confirm WMS-role tagging of intended requesters
Confirm the intended requesters are tagged `custentity_dbti_other_roles = "WMS"`
in NetSuite. The reused employee source filters on this attribute
(`Integration.NS/Implementations/Others/EmployeeIntegration.cs:27-32`), so
non-WMS employees will not be selectable. If requesters are not tagged, the
dropdown source must change and the design is incomplete.
_Requirements: 3.9_

---

## Phase 1 — DTOs (no behavior change)

### T-1.1
`Application.DataTransferObjects/Transactions/StockTransferRequest/StockTransferRequestInfoDTO.cs`
— add after line 21:

```csharp
public EmployeeNsDTO? RequestedBy { get; set; } = null;
```

`using Application.DataTransferObjects.Others;` already present (line 1).
_Requirements: 3.1, 3.5_

### T-1.2
`Web.BlazorServer/ViewModels/Transaction/StockTransferRequest/StockTransferRequestInfoVM.cs`
— add after line 21:

```csharp
public EmployeeNsVM? RequestedBy { get; set; } = null;
```

`using Web.BlazorServer.ViewModels.Others;` already present (line 3).
_Requirements: 3.1, 3.3_

### T-1.3
`Integration.NS/DataTransferObjects/StockTransferRequest/StockTransferRequestHeaderNSDTO.cs`
— add after line 36:

```csharp
public string RequestedByFirstName { get; set; } = string.Empty;
public string RequestedByLastName { get; set; } = string.Empty;
public int RequestedById { get; set; }
```

_Requirements: 3.7, 3.8_

**Checkpoint:** `dotnet build` — must compile with zero behavior change.

---

## Phase 2 — NetSuite read path

### T-2.1
`StockTransferRequestIntegration.cs` → `GetStockTransferRequest`.

Add to `.Select(...)` after line 178:

```csharp
("re.firstname", nameof(StockTransferRequestHeaderNSDTO.RequestedByFirstName)),
("re.lastname",  nameof(StockTransferRequestHeaderNSDTO.RequestedByLastName)),
("t.employee",   nameof(StockTransferRequestHeaderNSDTO.RequestedById)),
```

Add the join immediately after line 183:

```csharp
.LeftJoin("employee re", on: "re.id = t.employee")
```

**Do not** modify the existing `e` join.
_Requirements: 3.7_

### T-2.2
Same method — after the `Adapt` at line 196, add beside lines 198-206:

```csharp
dto.RequestedBy = nsdto.RequestedById > 0
    ? new EmployeeNsDTO
    {
        NsId = nsdto.RequestedById,
        FirstName = nsdto.RequestedByFirstName,
        LastName = nsdto.RequestedByLastName
    }
    : null;
```

Verify `using Application.DataTransferObjects.Others;` is present; add if not.
_Requirements: 3.4, 3.7_
_Design ref: D-9 — composing First/Last keeps `EmployeeNsVM.FullName` computable,
so the dropdown renders without a re-resolution round-trip._

---

## Phase 3 — NetSuite payload

### T-3.1
`StockTransferRequestIntegration.cs` → `CreateSTRPayload`, add beside line 378:

```csharp
employee = dto.RequestedBy is not null ? new { id = dto.RequestedBy.NsId.ToString() } : null,
```

_Requirements: 3.5_
_Covers create and update (P-9). Object form per D-5. `WhenWritingNull` (line 353)
omits it when null (P-2)._

**Checkpoint:** `dotnet build` clean. **No** handler, command, or interface
change in this phase — `CreateStockTransferRequest`
(`StockTransferRequestHandler.cs:74-86`) and `UpdateStockTransferRequest`
(lines 88-96) require no edits; `RequestedBy` flows via the existing `Adapt`
(P-10).

---

## Phase 4 — UI

### T-4.1
`STRForm.razor.cs` — add `readonly string ActionGetEmployees = "Get Employees";`
beside lines 46-49, and add the provider:

```csharp
async Task<(IEnumerable<EmployeeNsVM>, int)> EmployeeProvider(DataGridIntent intent)
    => await UserManagementHandler.GetAllEmployeesAsync(intent);
```

Add the injection beside line 41:

```csharp
[Inject] IUserManagementHandler UserManagementHandler { get; set; } = default!;
```

Add `using Web.BlazorServer.Handlers.Repositories.Administration.User;`.
_Requirements: 3.1, 3.9_

### T-4.2
`STRForm.razor` — insert into the **Details** fieldset, after line 93 (before
line 94):

```razor
<RadzenColumn SizeLG="6" SizeMD="12">
    <RadzenFormField Text="Requested By" class="rz-w-100">
        <ChildContent>
            <RadzenTextBox Disabled="@Disabled" Visible="@ReadOnly" ReadOnly
 Value="@(Model.RequestedBy?.FullName)" Placeholder="@(ReadOnly ? "Not set" : "Select an employee")" />
            <QuickVirtualizedDropdown Disabled="@Disabled" Visible="@(!ReadOnly)"
                Name="str_requested_by" TItem="EmployeeNsVM"
                ActionName="@ActionGetEmployees" DataGetter="@EmployeeProvider"
                @bind-Value:get="@Model.RequestedBy" @bind-Value:set="@(val => Model.RequestedBy = val)"
                FilterTarget="@nameof(EmployeeNsVM.FullName)"
                TextProperty="@nameof(EmployeeNsVM.FullName)"
                AllowFiltering AllowClear="false" />
        </ChildContent>
    </RadzenFormField>
</RadzenColumn>
```

_Requirements: 3.1, 3.3, 3.4_
`ReadOnly` toggling satisfies AC-5 with **no new editability logic** (D-7) —
verified: `View.razor:30` passes `ReadOnly`; `Update.razor:28` passes `EditMode`
without it. Uses `@Disabled` intentionally, unlike the existing
`Disabled="Disabled"` at lines 86-87 (untouched).

### T-4.3
`STRForm.razor.cs` → `HandleSubmit` (line 97), insert after the line-vendor check
at line 124:

```csharp
if (Model.RequestedBy is null)
{
    ToastService.Error("Please select a Requested By employee", "Error");
    return;
}
```

_Requirements: 3.2, 3.6_
_Single guard covers create (AC-2) and legacy update (AC-7), and returns before
`OnSubmit` so no NetSuite call is issued (P-5, P-6, AC-2)._

**Checkpoint:** `dotnet build` clean. Manual: dropdown renders and filters on all
three create forms (AC-1).

---

## Phase 5 — Grid

### T-5.1
`Integration.NS/DataTransferObjects/StockTransferRequest/StockTransferRequestDataGridNSDTO.cs`
— add after line 21:

```csharp
public string RequestedBy { get; set; } = string.Empty;
```

### T-5.2
`Application.DataTransferObjects/Transactions/StockTransferRequest/StockTransferRequestDataGridDTO.cs`
— add after line 20:

```csharp
public string RequestedBy { get; set; } = string.Empty;
```

_Requirements: 3.8_
_Flat string — grid columns bind to strings; `PreparedBy` at
`DataGridNSDTO:21` is precedent (D-9)._

### T-5.3
`StockTransferRequestIntegration.cs` — add to **all three** list queries:

```csharp
("CONCAT(re.firstname,CONCAT(' ',re.lastname))", nameof(StockTransferRequestDataGridNSDTO.RequestedBy)),
```

and `.LeftJoin("employee re", on: "re.id = t.employee")` beside each existing
`e` join:

- `GetIntercompanyTransferOrderList` — select after line 47, join after line 52
- `GetReturnsList` — select after line 89, join after line 93
- `GetTransferOrderList` — select after line 127, join after line 132

_Requirements: 3.8_
**Missing any one of the three fails AC-8 on that tab (P-4).** `Adapt` maps the
flat string automatically — no conversion code.

### T-5.4
`STRDataGrid.razor` — add beside the `PreparedBy` column (lines 78-81):

```razor
<RadzenDataGridColumn TItem="StockTransferRequestDataGridVM"
    Property="@nameof(StockTransferRequestDataGridVM.RequestedBy)"
    Title="Requested By" />
```

Match the filterable/sortable attributes of the `PreparedBy` column.
`StockTransferRequestDataGridVM` picks the property up via `Adapt` — verify no
explicit Mapster config for this type in
`Application.UseCases/Registers/MappingRegistration.cs`.
_Requirements: 3.8_
_Filtering requires T-5.3's projection or the filter silently returns nothing
(AC-9)._

---

## Phase 6 — Tests

Create
`Integration.NS.Tests/Implementations/Transactions/StockTransferRequestIntegrationTests.cs`.

Construct with:
`new StockTransferRequestIntegration(capturingClient, accessor, new SuiteQLQueryBuilderFactoryService())`
— pattern verified at
`Integration.NS.Tests/Implementations/Caching/CachedReferenceIntegrationTests.cs:33-38`.
**Do not** widen `CreateSTRPayload` visibility (D-8).

| # | Test | Properties / ACs |
|---|---|---|
| T-6.1 | `Create_payload_includes_employee_id_when_requested_by_set` | P-1, AC-3 |
| T-6.2 | `Create_payload_omits_employee_key_when_requested_by_null` | P-2 |
| T-6.3 | `Update_payload_includes_employee_id` | P-1, AC-6 |
| T-6.4 | `Header_query_selects_employee_id_and_joins_re_alias` | P-3, AC-4 |
| T-6.5 | `Header_query_keeps_e_alias_on_prepared_by` | P-3, P-10 |
| T-6.6 | `Transfer_order_list_query_selects_requested_by` | P-4, AC-8 |
| T-6.7 | `Intercompany_list_query_selects_requested_by` | P-4, AC-8 |
| T-6.8 | `Returns_list_query_selects_requested_by` | P-4, AC-8 |
| T-6.9 | `Update_payload_retains_employee_when_unchanged` | P-8, AC-10 |

_Requirements: 3.5, 3.6, 3.7, 3.8_

**Coverage limitation — state in a code comment:** no test project exists for
`Web.BlazorServer` or `Application.UseCases`, so `STRForm` markup and the
`HandleSubmit` guard (T-4.2, T-4.3) have **no automated coverage**. AC-1, AC-2,
AC-5, AC-7, and AC-11 require manual verification. Do not invent a test project.

---

## Phase 7 — Verification

### T-7.1
`dotnet build` — clean, no new warnings.

### T-7.2
`dotnet test` — full suite green, including all nine new tests and all
pre-existing caching/optimization tests.
_Requirements: 5.1, 5.2_

### T-7.3 — Manual
Verify each of AC-1 … AC-11 across all three STR types:

- Create each STR type with Requested By → NetSuite `employee` populated (AC-3)
- Legacy STR with no value → save blocked (AC-7)
- Draft edit changes value → persists (AC-6)
- Submitted STR → field read-only in View (AC-5)
- **Prepared By unchanged on create and update (AC-11)**
- Grid column populated on all three tabs; filter and sort work (AC-8, AC-9)
- Loaded Draft shows the stored employee name in the dropdown (AC-4, P-11)

### T-7.4 — Operational
Grid results are cached
(`Integration.NS/Implementations/Others/Caching/CachedStockTransferRequestIntegration.cs:19-29`).
Clear the cache on release so the new column appears immediately. Record in
`changelogs.md`.
_Requirements: 5.5_

---

## Requirement Traceability

| Requirement | Tasks |
|---|---|
| 3.1 Capture on creation | T-1.1, T-1.2, T-4.1, T-4.2 |
| 3.2 Mandatory on creation | T-4.3 |
| 3.3 Editable while draft | T-4.2 (no new logic — D-7) |
| 3.4 Read-only display | T-2.2, T-4.2 |
| 3.5 NetSuite persistence | T-0.1, T-1.1, T-3.1, T-6.1, T-6.3 |
| 3.6 Legacy records | T-4.3, T-6.2 |
| 3.7 Read path (id and name) | T-1.3, T-2.1, T-2.2, T-6.4, T-6.5 |
| 3.8 List grid column | T-1.3, T-5.1, T-5.2, T-5.3, T-5.4, T-6.6, T-6.7, T-6.8 |
| 3.9 Dropdown source | T-0.2, T-4.1 |
| 5.1–5.5 Preservation | Global Constraints 3-6, T-7.2, T-7.3 |

---

## Optional — NOT required for MVP, do not bundle

- **O-1** Subsidiary-scoping the employee dropdown via the existing
  `EmployeeNsVM.NsSubsidiaryId` — a provider change only (D-1). Deferred per
  Req 4.
- **O-2** Add `RequestedBy` to the printable NetSuite form (`PRINTABLE_URL_*`,
  `STRForm.razor.cs:65-66`) — requires NS-side script changes, outside this
  repository.
- **O-3** Reconcile `StockTransferRequestDataGridNSDTO.IsEditable:28` with
  `StockTransferRequestIntegration.cs:207` — a real inconsistency, deliberately
  preserved per Req 4. Should be its own ticket.
- **O-4** Correct `Disabled="Disabled"` → `Disabled="@Disabled"` at
  `STRForm.razor:86-87, 98, 112` — works today, but reads like a typo.