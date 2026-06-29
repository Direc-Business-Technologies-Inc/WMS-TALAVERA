using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.Utilities;
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
    [Inject] IStockTransferRequestPackingHandler strHandler { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public required DataGetterDelegate DataGetter { get; init; }

    [Parameter]
    public EventCallback OnAddClicked { get; set; }

    AppDataGrid<StockTransferRequestPackingDataGridVM> DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }
    TransferOrderStatusPackingVM? StatusFilter { get; set; } = null;

    readonly string ActionGetStockTransferRequests = EnumHelper.GetEnumDescription(AppActions.GetAllPackingStockTransferRequest);
    readonly string ActionGetTranferOrderStatuses = "Get Packing Transfer Order Status";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task StatusFilterChanged(TransferOrderStatusPackingVM? statusFilter)
    {
        if (statusFilter?.Id == StatusFilter?.Id) return;

        StatusFilter = statusFilter;
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
                    Property = "Date",
                    Direction = SortDirectionEnum.Descending
                });
            }
            if (StatusFilter is not null)
            {
                intent.Filters.Add(
                    DataGridFilterUtilities.Equal("StatusId", StatusFilter.Id)
                );
            }

            return await DataGetter(intent);
        }, AppActionOptionPresets.Loading(ActionGetStockTransferRequests));

        AppBusyService.SetBusy(ActionGetStockTransferRequests, false);
        return DataGridResultVM<StockTransferRequestPackingDataGridVM>.New(action.Result.data ?? [], action.Result.count);
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        DataGridSettings.CurrentPage = null;
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<(IEnumerable<TransferOrderStatusPackingVM>, int count)> TranferOrderStatusProvider(DataGridIntent intent)
    {
        intent.Filters.Add(
            DataGridFilterUtilities.In(nameof(TransferOrderStatusPackingVM.Id), new string[] { "B", "D" }
        ));
        return await strHandler.GetTransferOrderStatuses(intent);
    }

    void ViewSTR(StockTransferRequestPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.StockTransferRequestView + $"?ref={item.ReferenceNumber}");
    }

    async Task AddButtonPressed()
    {
        if (OnAddClicked.HasDelegate) await OnAddClicked.InvokeAsync();
    }

    public delegate Task<(IEnumerable<StockTransferRequestPackingDataGridVM> data, int count)> DataGetterDelegate(DataGridIntent intent);
}
