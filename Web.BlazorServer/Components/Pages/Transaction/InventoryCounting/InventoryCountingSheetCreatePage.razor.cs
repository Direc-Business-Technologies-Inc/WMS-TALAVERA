using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;

public partial class InventoryCountingSheetCreatePage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public Guid? Document { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] TooltipService TooltipService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    bool IsLoadingData => AppBusyService.IsBusy(ActionView);

    readonly string ActionView = EnumHelper.GetEnumDescription(AppActions.ViewInventoryCountingDocument);
    readonly string ActionCreate = EnumHelper.GetEnumDescription(AppActions.CreateInventoryCountingSheet);
    #endregion Primitives

    #region Data Structures
    InventoryCountingVM? ParentDocument { get; set; }
    AppTable<InventoryCountingSheetLineVM> SheetLinesTable { get; set; } = default!;
    DataGridSettings SheetLinesTableSettings { get; set; } = new();
    #endregion Data Structures

    #region Overrides
    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionView, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override Task InitializeEditing() => Task.CompletedTask;
    protected override Task CancelEditing() => GoBack();

    protected override async Task HandleSubmit()
    {
        var lines = FormData.SheetLines.Where(l => l.Quantity > 0).ToList();

        if (lines.Count == 0)
        {
            ToastService.Warning("Please enter a count quantity for at least one item before saving.");
            return;
        }

        FormData.SheetLines = lines;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreate, true);
            return await InventoryCountingHandler.CreateInventoryCountingSheetAsync(FormData);
        }, AppActionOptionPresets.Confirmed(ActionCreate));

        AppBusyService.SetBusy(ActionCreate, false);

        action.OnSuccess(async _ =>
        {
            UnsavedChangesService.MarkClean();
            NavManager.NavigateTo($"/transactions/inventory/inventory-counting/view?Id={Document}", true);
        });
    }
    #endregion Overrides

    #region Custom Functions
    async Task LoadDataAsync()
    {
        if (!Document.HasValue)
        {
            AppBusyService.SetBusy(ActionView, false);
            return;
        }

        GridSettingsLoaded = true;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionView, true);
            return await InventoryCountingHandler.GetInventoryCountingDocumentAsync(Document.Value);
        }, AppActionOptionPresets.Loading(ActionView));

        AppBusyService.SetBusy(ActionView, false);

        action.OnSuccess(result =>
        {
            if (result is null)
            {
                ToastService.Error("Inventory Counting document not found.");
                return Task.CompletedTask;
            }

            ParentDocument = result;

            // Map document lines → sheet lines (ISBN carried over, count starts at 0)
            FormData.InventoryCountingDocumentId = result.Id;
            FormData.SheetLines = [.. result.DocumentLines.Select(dl => new InventoryCountingSheetLineVM
            {
                ItemCode = dl.ItemCode,
                ItemName = dl.ItemName,
                Quantity = 0,
                UoMCode = dl.UoMCode,
                UoMValue = dl.UoMValue,
                UoMName = dl.UoMName,
                ISBN = dl.ISBN,
            })];

            FormData.Counter = new UserDetailVM
            {
                UserId = CurrentUserService.UserId,
                Name = CurrentUserService.UserName,
            };
            FormData.SubmittedDate = DateTime.Today;

            AdaptToClone();
            return Task.CompletedTask;
        });
    }

    async Task OpenScannerDialog()
    {
        await DialogService.OpenAsync<InventoryCountingScanner>(
            "Scan Barcode",
            new Dictionary<string, object>
            {
                { "OnScan", EventCallback.Factory.Create<string>(this, HandleScanResult) }
            },
            options: new Radzen.DialogOptions
            {
                Width = "400px",
                CloseDialogOnOverlayClick = false,
            });
    }

    void HandleScanResult(string isbn)
    {
        var line = FormData.SheetLines.FirstOrDefault(l =>
            !string.IsNullOrWhiteSpace(l.ISBN) &&
            l.ISBN.Equals(isbn, StringComparison.OrdinalIgnoreCase));

        if (line is null)
        {
            ToastService.Warning($"No item found for barcode: {isbn}");
            return;
        }

        line.Quantity += 1;
        _ = SheetLinesTable.DataGrid.Reload();
        InvokeAsync(StateHasChanged);
    }

    void ShowISBNTooltip(ElementReference el, string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return;
        TooltipService.Open(el, isbn, new TooltipOptions { Position = TooltipPosition.Left, Duration = null });
    }

    async Task GoBack()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Sheet Creation"))
                return;

        NavManager.NavigateTo($"/transactions/inventory/inventory-counting/view?Id={Document}", true);
    }
    #endregion Custom Functions
}
