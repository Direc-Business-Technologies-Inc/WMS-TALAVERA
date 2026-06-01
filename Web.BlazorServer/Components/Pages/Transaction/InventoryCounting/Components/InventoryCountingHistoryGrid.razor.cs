using Domain.Entities.Enums.Transaction.InventoryCounting;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;

public partial class InventoryCountingHistoryGrid
{
    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<InventoryCountingDataGridVM> DataGrid { get; set; } = default!;
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

    async Task<DataGridResultVM<InventoryCountingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        intent.Filters.Add(new AppFilterDescriptor
        {
            LogicalOperator = LogicalOperatorEnum.AND,
            Property = nameof(InventoryCountingDataGridVM.Status),
            Value = new List<InventoryCountingDocumentStatus>
            {
                InventoryCountingDocumentStatus.Saved,
                InventoryCountingDocumentStatus.Posted
            },
            ComparisonOperator = ComparisonOperatorEnum.In,
        });

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetAll, true);
            var response = await InventoryCountingHandler.GetInventoryCountingDataGridAsync(intent);
            return response;
        }, AppActionOptionPresets.Loading(ActionGetAll));

        AppBusyService.SetBusy(ActionGetAll, false);
        return DataGridResultVM<InventoryCountingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewDocument(InventoryCountingDataGridVM doc) =>
        NavManager.NavigateTo($"/transactions/inventory/inventory-counting/view?Id={doc.Id}", true);
}
