using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components;

partial class STRDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IStockTransferRequestHandler strHandler { get; set; } = default!;
    [Parameter] public string? TableId { get; set; } = "str_datagrid";

    [Parameter][EditorRequired]
    public required DataGetterDelegate DataGetter { get; init; }

    [Parameter]
    public EventCallback OnAddClicked { get; set; }
    [Parameter]
    public bool ShowToSubsidiary { get; set; } = true; // lmaoooooo idc
    [Parameter]
    public bool ShowPurchaseCategory { get; set; } = true;

    AppDataGrid<StockTransferRequestDataGridVM> DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }
    TransferOrderStatusVM? StatusFilter { get; set; } = null;

    readonly string ActionGetStockTransferRequests = "get things from db";
    readonly string ActionGetTranferOrderStatuses = "Get Transfer Order Status";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task StatusFilterChanged(TransferOrderStatusVM? statusFilter)
    {
        if (statusFilter?.Id == StatusFilter?.Id) return;

        StatusFilter = statusFilter;
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<StockTransferRequestDataGridVM>> LoadDataAsync(DataGridIntent intent)
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

                intent.Sorts.Add(new()
                {
                    Property = "ReferenceNumber",
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

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetStockTransferRequests));

        AppBusyService.SetBusy(ActionGetStockTransferRequests, false);
        return DataGridResultVM<StockTransferRequestDataGridVM>.New(action.Result.data ?? [], action.Result.count);
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        DataGridSettings.CurrentPage = null;
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<(IEnumerable<TransferOrderStatusVM>, int count)> TranferOrderStatusProvider(DataGridIntent intent)
    {
        return await strHandler.GetTransferOrderStatuses(intent);
    }

    void ViewSTR(StockTransferRequestDataGridVM item)
    {
        NavManager.NavigateTo(STRRoutes.View + $"?ref={item.ReferenceNumber}");
    }

    async Task AddButtonPressed()
    {
        if (OnAddClicked.HasDelegate) await OnAddClicked.InvokeAsync();
    }

    private async Task OnStatusFilterChanged(
    TransferOrderStatusVM? value,
    RadzenDataGridColumn<StockTransferRequestDataGridVM> column)
    {
        StatusFilter = value;

        await ApplyStatusFilter(column);
    }

    async Task ApplyStatusFilter(RadzenDataGridColumn<StockTransferRequestDataGridVM> column)
    {

        column.ClearFilters();
        if (StatusFilter is null)
        {
            await DataGrid.DataGrid.Reload();
            return;
        }

        column.SetFilterOperator(FilterOperator.Equals);
        column.SetFilterValue(StatusFilter.Name);
        column.SetLogicalFilterOperator(LogicalFilterOperator.And);

        await DataGrid.DataGrid.Reload();
    }

    public delegate Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> DataGetterDelegate(DataGridIntent intent);
}
