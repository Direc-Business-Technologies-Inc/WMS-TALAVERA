using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsIssue;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsIssue.Components;

public partial class PendingDataGrid
{
    [Inject] IGoodsIssueHandler GoodsIssueHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<GoodsIssueDataGridVM> GoodsIssueDataGrid { get; set; }
    DataGridSettings GoodsIssueDataGridSettings { get; set; }

    string ActionGetGoodsIssues { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllGoodsIssues);

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
        await GridSettingsService.SetGridSettings(GoodsIssueDataGrid.DataGrid, settings => GoodsIssueDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsIssueDataGrid.DataGrid.ReloadSettings();
        await GoodsIssueDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<GoodsIssueDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetGoodsIssues, true);

            var response = await GoodsIssueHandler.GetGoodsIssueDataGridAsync(intent, "W");

            return response;

        }, AppActionOptionPresets.Loading(ActionGetGoodsIssues));

        AppBusyService.SetBusy(ActionGetGoodsIssues, false);
        return DataGridResultVM<GoodsIssueDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewGoodsIssue(GoodsIssueDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/inventory/goods-issue/view?ref={purchaseOrder.DocEntry}&draft=1", true);
    void CreateGoodsIssue() => NavManager.NavigateTo($"/transactions/inventory/goods-issue/create", true);
}
