using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReceipt;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReceipt.Components;

public partial class PendingDataGrid
{
    [Inject] IGoodsReceiptHandler GoodsReceiptHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<GoodsReceiptDataGridVM> GoodsReceiptDataGrid { get; set; }
    DataGridSettings GoodsReceiptDataGridSettings { get; set; }

    string ActionGetGoodsReceipts { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllGoodsReceipts);

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
        await GridSettingsService.SetGridSettings(GoodsReceiptDataGrid.DataGrid, settings => GoodsReceiptDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsReceiptDataGrid.DataGrid.ReloadSettings();
        await GoodsReceiptDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<GoodsReceiptDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetGoodsReceipts, true);

            var response = await GoodsReceiptHandler.GetGoodsReceiptDataGridAsync(intent, "W");

            return response;

        }, AppActionOptionPresets.Loading(ActionGetGoodsReceipts));

        AppBusyService.SetBusy(ActionGetGoodsReceipts, false);
        return DataGridResultVM<GoodsReceiptDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewGoodsReceipt(GoodsReceiptDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/inventory/goods-receipt/view?ref={purchaseOrder.DocEntry}&draft=1", true);
    void CreateGoodsReceipt() => NavManager.NavigateTo($"/transactions/inventory/goods-receipt/create", true);
}
