using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReturn;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReturn.Components;

public partial class GoodsReturnRequestGrid
{
    [Inject] IGoodsReturnHandler GoodsReturnHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<GRRDataGridVM> GoodsReturnRequestDataGrid { get; set; }
    DataGridSettings GoodsReturnRequestDataGridSettings { get; set; }

    string ActionGetGoodsReturnRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllGoodsReturnRequests);

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
        await GridSettingsService.SetGridSettings(GoodsReturnRequestDataGrid.DataGrid, settings => GoodsReturnRequestDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsReturnRequestDataGrid.DataGrid.ReloadSettings();
        await GoodsReturnRequestDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<GRRDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetGoodsReturnRequests, true);

            var response = await GoodsReturnHandler.GetGoodsReturnRequestDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetGoodsReturnRequests));

        AppBusyService.SetBusy(ActionGetGoodsReturnRequests, false);
        return DataGridResultVM<GRRDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewGoodsReturnRequest(GRRDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/goods-return/request/view?ref={purchaseOrder.DocEntry}", true);
}
