using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.STR.Component;

partial class STRPackingDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IStockTransferRequestPackingHandler StrHandler { get; set; } = default!;

    AppDataGrid<StockTransferRequestPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();

    string ActionGetStockTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingStockTransferRequest);

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
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<StockTransferRequestPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetStockTransferRequests, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = nameof(StockTransferRequestPackingDataGridVM.Date),
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await StrHandler.GetStockTransferRequestsList(intent, CurrentUserService.NsSubsidiaryId);
        }, AppActionOptionPresets.Loading(ActionGetStockTransferRequests));

        AppBusyService.SetBusy(ActionGetStockTransferRequests, false);
        return DataGridResultVM<StockTransferRequestPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSTR(StockTransferRequestPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.StockTransferRequestView + $"?ref={item.ReferenceNumber}", true);
    }
}
