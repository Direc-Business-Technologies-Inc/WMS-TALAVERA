using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class PurchaseOrderGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<PurchaseOrderDataGridVM> PurchaseOrderDataGrid { get; set; }
    DataGridSettings PurchaseOrderDataGridSettings { get; set; }

    string ActionGetPurchaseOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPurchaseOrders);

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
        await GridSettingsService.SetGridSettings(PurchaseOrderDataGrid.DataGrid, settings => PurchaseOrderDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await PurchaseOrderDataGrid.DataGrid.ReloadSettings();
        await PurchaseOrderDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<PurchaseOrderDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrders, true);

            var response = await ReceivingHandler.GetPurchaseOrderDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrders));

        AppBusyService.SetBusy(ActionGetPurchaseOrders, false);
        return DataGridResultVM<PurchaseOrderDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewPurchaseOrder(PurchaseOrderDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/view?ref={purchaseOrder.DocEntry}", true);
}
