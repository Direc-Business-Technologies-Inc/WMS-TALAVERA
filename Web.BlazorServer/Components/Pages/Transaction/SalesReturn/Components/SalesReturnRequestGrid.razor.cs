using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.SalesReturn;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.SalesReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SalesReturn.Components;

public partial class SalesReturnRequestGrid
{
    [Inject] ISalesReturnHandler SalesReturnHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<SalesReturnRequestDataGridVM> SalesReturnRequestDataGrid { get; set; } = default!;
    DataGridSettings SalesReturnRequestDataGridSettings { get; set; } = new();

    string ActionGetSalesReturnRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllSalesReturnRequests);

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
        await GridSettingsService.SetGridSettings(SalesReturnRequestDataGrid.DataGrid, settings => SalesReturnRequestDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await SalesReturnRequestDataGrid.DataGrid.ReloadSettings();
        await SalesReturnRequestDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<SalesReturnRequestDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetSalesReturnRequests, true);
            var response = await SalesReturnHandler.GetSalesReturnRequestDataGridAsync(intent);
            return response;
        }, AppActionOptionPresets.Loading(ActionGetSalesReturnRequests));

        AppBusyService.SetBusy(ActionGetSalesReturnRequests, false);
        return DataGridResultVM<SalesReturnRequestDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSalesReturnRequest(SalesReturnRequestDataGridVM item) => NavManager.NavigateTo($"/transactions/sales/sales-return/request/view?ref={item.DocEntry}", true);
}
