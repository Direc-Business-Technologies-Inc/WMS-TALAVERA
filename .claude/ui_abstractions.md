# UI Abstractions & Workflow

## Overview

All interactive UI operations in this project flow through a set of shared abstractions.
Every component inherits these via `BaseComponent`. Form pages additionally inherit
`BaseForm<TItem>`. Understanding this system is required before writing any new page.

---

## BaseComponent — Shared DI

All components inherit `BaseComponent`, which pre-injects:

| Property | Type | Purpose |
|---|---|---|
| `AppActionFactory` | `IAppActionFactory` | Run UI actions with loading, confirmation, and toast |
| `AppBusyService` | `IBusyService` | Track and check busy state by action key |
| `NavManager` | `NavigationManager` | Client-side navigation |
| `DialogService` | `DialogService` | Radzen modal dialogs |
| `ToastService` | `IToastService` | Toast notifications |
| `AlertService` | `IAlertService` | Alert banners |
| `JS` | `IJSRuntime` | JavaScript interop |
| `CurrentUserService` | `ICurrentUserService` | Logged-in user info |
| `Can` | `AuthorizationHelper` | Permission checks |
| `AuthenticationService` | `AppAuthenticationService` | Auth state |

Never re-inject any of these in a component — they are already available via `BaseComponent`.

---

## BaseForm<TItem> — Form Page Base

`BaseForm<TItem>` extends `BaseComponent`. All Create/View/Update (CVU) pages inherit
from this instead of `BaseComponent` directly.

```
BaseComponent
  └── BaseForm<TItem>   ← CVU pages inherit this
```

### Properties

| Property | Type | Purpose |
|---|---|---|
| `FormData` | `TItem` | Live form data — bound to `RadzenTemplateForm` |
| `FormDataClone` | `TItem` | Snapshot of FormData — used to restore on cancel |
| `EditContext` | `EditContext` | Tracks field changes and validation state |
| `TemplateForm` | `RadzenTemplateForm<TItem>` | Reference to the Radzen form |
| `SubmitBtn` | `RadzenButton` | Reference to the submit button |

### Abstract Methods (must implement)

```csharp
protected abstract Task InitializeEditing();  // populate FormData when opening an existing record
protected abstract Task CancelEditing();       // handle cancel — typically AdaptToForm() + navigate
protected abstract Task HandleSubmit();        // handle form submission
```

### Key Methods

```csharp
AdaptToClone()           // snapshot: FormData → FormDataClone (call before editing starts)
AdaptToForm()            // restore: FormDataClone → FormData (call on cancel)
ResetFormContext()        // rebuild EditContext after submit (clears validation state)
NotifyAllFieldsChanged() // trigger validation on all fields (call before submit to surface errors)
OnFieldChanged(name)     // manually notify EditContext that a specific field changed
```

### Unsaved Changes
`HandleFormChange` is hooked to `EditContext.OnFieldChanged` automatically.
Any field modification calls `UnsavedChangesService.MarkDirty()`, which activates
the unsaved changes guard on navigation.

### Typical CVU Lifecycle

```
OnInitializedAsync:
  PageActionType = Create / Update / View (from route or parameter)
  if Update or View → call InitializeEditing() → populate FormData → AdaptToClone()

User edits fields → EditContext fires OnFieldChanged → UnsavedChangesService.MarkDirty()

User submits → NotifyAllFieldsChanged() → if valid → HandleSubmit()
  → AppActionFactory.RunAsync(handler call, options)
  → on success → ResetFormContext() or navigate away

User cancels → CancelEditing() → AdaptToForm() or navigate
```

---

## AppActions — Named Action Registry

`AppActions` (in `Web.BlazorServer/Defaults/AppActions.cs`) is an enum that names
every operation in the system. Each value carries a `[Description]` attribute with
the human-readable label used in loading indicators, toast messages, and busy state keys.

```csharp
[Description("Create Goods Receipt PO")]
CreateGoodsReceiptPO,
```

**Always use `AppActions` as the `ActionName`** in `AppActionFactory`, `IBusyService`,
and `AppDataGrid`. When adding a new feature, register its actions here first.

---

## IBusyService — Loading State Tracker

Keyed dictionary of busy states scoped to the Blazor circuit.

```csharp
AppBusyService.SetBusy(key, true);   // mark busy
AppBusyService.IsBusy(key);          // check one key
AppBusyService.IsAnyBusy();          // check if anything is busy
AppBusyService.BusyChanged           // event — fires on any state change
```

Use `AppActions.XxxAction.GetDescription()` as the key.
`AppActionFactory` handles `SetBusy`/clear automatically when `ShowLoadingIndicator = true`.

---

## AppActionFactory — UI Action Runner

Wraps any async operation with confirmation, busy state, and toast feedback.

### Overloads

```csharp
// Returns a value
Task<AppAction<T>> RunAsync<T>(Func<Task<T?>> func, AppActionFactoryOptions options);

// Fire-and-forget
Task<AppAction> RunAsync(Func<Task> func, AppActionFactoryOptions options);
```

### AppActionFactoryOptions

```csharp
new AppActionFactoryOptions
{
    ActionName = AppActions.CreateGoodsReceiptPO.GetDescription(), // required
    ShowLoadingIndicator = true,    // default: true
    ShowConfirmationDialog = true,  // default: true
    ShowToastOnSuccess = true,      // default: true
    ShowToastOnFailure = true,      // default: true
}
```

### AppAction Result

```csharp
var result = await AppActionFactory.RunAsync(
    () => ReceivingHandler.PostGoodsReceiptPOAsync(payload),
    new AppActionFactoryOptions { ActionName = AppActions.CreateGoodsReceiptPO.GetDescription() }
);

result
    .OnSuccess(_ => { /* handle */ return Task.CompletedTask; })
    .OnFailure(ex => { /* handle */ return Task.CompletedTask; });
```

`AppAction<T>` exposes: `IsSuccess`, `IsCancelled`, `Result`, `Exception`, `ErrorMessage`.

---

## AppActionFactory — Preset Selection Rules

Always choose the preset based on the **nature of the operation**, not personal preference.

| Situation | Preset | Behaviour |
|---|---|---|
| Fetching data (page load, dropdowns, grids) | `AppActionOptionPresets.Loading(actionName)` | No confirm dialog · error toast on failure only |
| Posting a form (create / update / delete) | `AppActionOptionPresets.Confirmed(actionName)` | Confirm dialog required · success + failure toasts |
| Background refresh / silent side-effect | `AppActionOptionPresets.Silent(actionName)` | No confirm dialog · success + failure toasts |

### Busy-State Convention Inside Delegates

- Call `AppBusyService.SetBusy(actionName, true)` **inside** the delegate immediately before the handler call
- Clear it (`false`) **after** `AppActionFactory.RunAsync()` returns in the outer scope — not inside the delegate — so the factory's `finally` block can still observe it
- Always derive the key from `AppActions` via `EnumHelper.GetEnumDescription(AppActions.XxxAction)` — never use raw strings

```csharp
// DATA FETCH — Loading preset
readonly string ActionGetFoo = EnumHelper.GetEnumDescription(AppActions.GetFoo);

var action = await AppActionFactory.RunAsync(async () =>
{
    AppBusyService.SetBusy(ActionGetFoo, true);
    return await FooHandler.GetFooAsync(id);
}, AppActionOptionPresets.Loading(ActionGetFoo));

AppBusyService.SetBusy(ActionGetFoo, false);          // clear in outer scope

action.OnSuccess(async (result) =>
{
    if (result is null) ToastService.Error("Not found");
    else result.Adapt(FormData);
});

// FORM POST — Confirmed preset
readonly string ActionCreateFoo = EnumHelper.GetEnumDescription(AppActions.CreateFoo);

var action = await AppActionFactory.RunAsync(async () =>
{
    AppBusyService.SetBusy(ActionCreateFoo, true);
    return await FooHandler.PostFooAsync(FormData);
}, AppActionOptionPresets.Confirmed(ActionCreateFoo));

AppBusyService.SetBusy(ActionCreateFoo, false);        // clear in outer scope

action.OnSuccess(async (_) =>
{
    NavManager.NavigateTo("/target/path");
});
```

---

## Navigation Guard — HasUnsavedChanges

All CVU pages that allow editing (Create or Update mode) **must** guard every navigation
function that exits the page with an unsaved-changes confirmation prompt.

### Rules

- Navigation functions that include the guard must be `async Task` — not `void`
- Apply the guard only for modes where the form is editable; skip in View-only mode
- `UnsavedChangesService` is already wired by `BaseForm<T>` — do not call `MarkDirty()` manually unless a field is not bound to the `EditContext`
- Call `UnsavedChangesService.MarkClean()` on successful submit before navigating away
- View-only pages (no form mutations possible) do not require a guard

### Standard Pattern

```csharp
// Navigation function on a Create/Update CVU page
async Task Back()
{
    if (UnsavedChangesService.HasChanges && Creating)   // replace Creating with Editing as appropriate
        if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel <Document> Creation"))
            return;

    NavManager.NavigateTo("/target/path", true);
}
```

### When the guard is NOT needed

- List / data-grid pages (no form)
- Tab-switcher pages (no form)
- View-only CVU pages (fields are read-only; no edits can be made)

---

## AppDataGrid<TItem> — Data Grid Wrapper

Wraps Radzen's `RadzenDataGrid` with server-side loading, grid settings persistence,
busy state integration, and skeleton loading.

### Key Parameters

| Parameter | Type | Purpose |
|---|---|---|
| `DataGetter` | `Func<DataGridIntent, Task<DataGridResultVM<TItem>>>` | Server-side data loader |
| `GridColumns` | `RenderFragment` | Column definitions |
| `ActionName` | `string` | Ties to `IBusyService` — shows loading when busy |
| `DefaultFilters` | `IEnumerable<AppFilterDescriptor>` | Filters always applied |
| `ClientSide` | `bool` | Use local `Data` list; skip server call |
| `SelectedItems` | `IList<TItem>` | Two-way bound selected rows |
| `OnRowDoubleClick` | `EventCallback` | Row double-click handler |
| `OnRowSelect` | `EventCallback<TItem>` | Row selection handler |
| `GridSettingsLoaded` | `bool` | Two-way bound; `true` after settings load |

### DataGridResultVM<T>

Standard return shape from any handler feeding a grid:

```csharp
DataGridResultVM<T>.New(items, count)
// Properties: Items (IEnumerable<T>), Count (int)
```

All server-side grid query handlers return `(IEnumerable<TDto> Data, int Count)`,
which maps to this VM inside the component.

### Typical Usage

```razor
<AppDataGrid TItem="PurchaseOrderDataGridVM"
             ActionName="@AppActions.GetAllPurchaseOrders.GetDescription()"
             DataGetter="@GetPurchaseOrdersAsync"
             @bind-GridSettingsLoaded="GridSettingsLoaded"
             @bind-SelectedItems="SelectedItems"
             OnRowDoubleClick="@OnRowDoubleClick">
    <GridColumns>
        <RadzenDataGridColumn Property="DocNum" Title="Doc #" />
    </GridColumns>
</AppDataGrid>
```

---

## AppGridSettings — Grid Settings Panel

Passed the grid reference and settings object; lets users configure page size,
filter mode, column picking, resizing, and other display options.
Settings are persisted per grid via `IGridSettingsService`.

---

## PageActionTypeEnum — Form Mode

```csharp
public enum PageActionTypeEnum { Create, Update, View }
```

Controls field editability, button visibility, and validation behavior on form pages.
Set on initialization based on route parameters or navigation context.

---

## Typical UI Workflow

```
1. User triggers action (button click, row double-click)
2. Component calls AppActionFactory.RunAsync(...)
3. AppActionFactory:
   a. Shows confirmation dialog (if enabled)
   b. Calls AppBusyService.SetBusy(ActionName, true)
   c. AppDataGrid detects IsBusy → shows loading indicator
   d. Executes the delegate (calls IXxxHandler → MediatR)
   e. Clears busy state
   f. Shows success or failure toast
4. Component handles result via .OnSuccess() / .OnFailure()
5. Component calls StateHasChanged() or grid reload as needed
```
