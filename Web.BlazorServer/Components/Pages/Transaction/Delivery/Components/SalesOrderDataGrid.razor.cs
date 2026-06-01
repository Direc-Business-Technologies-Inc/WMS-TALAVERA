using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Delivery;

namespace Web.BlazorServer.Components.Pages.Transaction.Delivery.Components;

public partial class SalesOrderDataGrid
{
    [Inject] IDeliveryHandler DeliveryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<SalesOrderDataGridVM> SalesOrderGrid { get; set; } = default!;
    DataGridSettings SalesOrderDataGridSettings { get; set; } = new();

    string ActionGetAllSalesOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllSalesOrders);

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
        await GridSettingsService.SetGridSettings(SalesOrderGrid.DataGrid, settings => SalesOrderDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await SalesOrderGrid.DataGrid.ReloadSettings();
        await SalesOrderGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<SalesOrderDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetAllSalesOrders, true);

            var response = await DeliveryHandler.GetSalesOrderDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetAllSalesOrders));

        AppBusyService.SetBusy(ActionGetAllSalesOrders, false);
        return DataGridResultVM<SalesOrderDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSalesOrder(SalesOrderDataGridVM salesOrder) => NavManager.NavigateTo($"/transactions/sales/delivery/sales-order/view?ref={salesOrder.DocEntry}");
}
