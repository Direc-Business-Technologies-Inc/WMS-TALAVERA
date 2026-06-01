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

public partial class DeliveryDataGrid
{
    [Inject] IDeliveryHandler DeliveryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<DeliveryDataGridVM> DeliveryGrid { get; set; } = default!;
    DataGridSettings DeliveryDataGridSettings { get; set; } = new();

    string ActionGetAllDeliveries { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllDeliveries);

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
        await GridSettingsService.SetGridSettings(DeliveryGrid.DataGrid, settings => DeliveryDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DeliveryGrid.DataGrid.ReloadSettings();
        await DeliveryGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<DeliveryDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetAllDeliveries, true);

            var response = await DeliveryHandler.GetDeliveryDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetAllDeliveries));

        AppBusyService.SetBusy(ActionGetAllDeliveries, false);
        return DataGridResultVM<DeliveryDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewDelivery(DeliveryDataGridVM delivery) => NavManager.NavigateTo($"/transactions/sales/delivery/view?ref={delivery.DocEntry}");
}
