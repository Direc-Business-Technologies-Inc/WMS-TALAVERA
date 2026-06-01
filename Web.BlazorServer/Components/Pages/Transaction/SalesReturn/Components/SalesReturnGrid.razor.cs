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

public partial class SalesReturnGrid
{
    [Inject] ISalesReturnHandler SalesReturnHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<SalesReturnDataGridVM> SalesReturnDataGrid { get; set; } = default!;
    DataGridSettings SalesReturnDataGridSettings { get; set; } = new();

    string ActionGetSalesReturns { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllSalesReturns);

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
        await GridSettingsService.SetGridSettings(SalesReturnDataGrid.DataGrid, settings => SalesReturnDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await SalesReturnDataGrid.DataGrid.ReloadSettings();
        await SalesReturnDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<SalesReturnDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetSalesReturns, true);
            var response = await SalesReturnHandler.GetSalesReturnDataGridAsync(intent);
            return response;
        }, AppActionOptionPresets.Loading(ActionGetSalesReturns));

        AppBusyService.SetBusy(ActionGetSalesReturns, false);
        return DataGridResultVM<SalesReturnDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSalesReturn(SalesReturnDataGridVM item) => NavManager.NavigateTo($"/transactions/sales/sales-return/view?ref={item.DocEntry}", true);
    void CreateSalesReturn() => NavManager.NavigateTo("/transactions/sales/sales-return/create", true);
}
