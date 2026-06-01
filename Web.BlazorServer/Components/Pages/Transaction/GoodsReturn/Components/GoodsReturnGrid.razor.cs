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

public partial class GoodsReturnGrid
{
    [Inject] IGoodsReturnHandler GoodsReturnHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<GoodsReturnDataGridVM> GoodsReturnDataGrid { get; set; }
    DataGridSettings GoodsReturnDataGridSettings { get; set; }

    string ActionGetGoodsReturns { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllGoodsReturns);

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
        await GridSettingsService.SetGridSettings(GoodsReturnDataGrid.DataGrid, settings => GoodsReturnDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsReturnDataGrid.DataGrid.ReloadSettings();
        await GoodsReturnDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<GoodsReturnDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetGoodsReturns, true);

            var response = await GoodsReturnHandler.GetGoodsReturnDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetGoodsReturns));

        AppBusyService.SetBusy(ActionGetGoodsReturns, false);
        return DataGridResultVM<GoodsReturnDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewGoodsReturn(GoodsReturnDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/goods-return/view?ref={purchaseOrder.DocEntry}", true);
    void CreateGoodsReturn() => NavManager.NavigateTo($"/transactions/purchasing/goods-return/create", true);
}
