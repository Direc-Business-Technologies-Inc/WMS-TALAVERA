using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel.InventoryCounting;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;

public partial class InventoryCountingOpenGrid
{
    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<InventoryCountingVM> DataGrid { get; set; } = default!;
    DataGridSettings GridSettings { get; set; } = new();

    string ActionGetAll { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllInventoryCountingDocuments);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => GridSettings = settings ?? new());
        GridSettingsLoaded = true;
        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<InventoryCountingVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetAll, true);
            var response = await InventoryCountingHandler.GetStartedInventoryCountingAsync(intent);
            return response;
        }, AppActionOptionPresets.Loading(ActionGetAll));

        AppBusyService.SetBusy(ActionGetAll, false);
        return DataGridResultVM<InventoryCountingVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewDocument(InventoryCountingVM doc) =>
        NavManager.NavigateTo($"/transactions/inventory/inventory-counting/ns/view?OrderNumber={Uri.EscapeDataString(doc.OrderNumber)}", true);
}
