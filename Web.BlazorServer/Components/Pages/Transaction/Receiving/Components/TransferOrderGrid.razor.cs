using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class TransferOrderGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public string? Source { get; set; } = "purchaseorder";

    AppDataGrid<TransferOrderDataGridVM> TransferOrderDataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }

    string ActionGetPurchaseOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllTransferOrders);

    TransferOrderStatusVM? StatusFilter = null;
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    PrintStatusView PrintStatusView = default!;
    bool initialGridLoadSucceeded;
    bool initialCountRefreshRequested;

    Task OnGridDataLoaded()
    {
        if (!initialGridLoadSucceeded || initialCountRefreshRequested) return Task.CompletedTask;
        initialCountRefreshRequested = true;
        _ = InvokeAsync(async () => { await Task.Yield(); await PrintStatusView.RefreshCountsAsync(); });
        return Task.CompletedTask;
    }

    async Task<int> PrintCountAsync(PrintStatus status)
    {
        var intent = new DataGridIntent { Skip = 0, Take = 1 };
        ReceivingReceiptPrintStatusFilter.ApplyToReceipts(intent, status, "STR");
        if (DateRangeStart.HasValue) intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("tcf.trandate", DateRangeStart.Value.Date));
        if (DateRangeEnd.HasValue) intent.Filters.Add(DataGridFilterUtilities.LessThan("tcf.trandate", DateRangeEnd.Value.Date.AddDays(1)));
        ApplyStatusCountFilter(intent);
        var (_, count) = await ReceivingHandler.GetItemReceiptsDatagridAsync(intent);
        return count;
    }

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        DateRangeStart = range.Start;
        DateRangeEnd = range.End;
        await ReloadFirstPageAsync();
    }

    async Task ReloadFirstPageAsync()
    {
        if (TransferOrderDataGrid.DataGrid.CurrentPage == 0)
            await TransferOrderDataGrid.DataGrid.Reload();
        else
            await TransferOrderDataGrid.DataGrid.GoToPage(0);
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        if (TransferOrderDataGrid.DataGrid.CurrentPage == 0)
            await TransferOrderDataGrid.DataGrid.Reload();
        else
            await TransferOrderDataGrid.DataGrid.GoToPage(0);
    }

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
        await GridSettingsService.SetGridSettings(TransferOrderDataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await TransferOrderDataGrid.DataGrid.ReloadSettings();
        await TransferOrderDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<TransferOrderDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        ReceivingReceiptPrintStatusFilter.ApplyToTransactions(intent, SelectedPrintStatus);
        PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrders, true);

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

            var result = await ReceivingHandler.GetTransferOrderDataGridAsync(intent);
            return result;

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrders));

        initialGridLoadSucceeded = action.IsSuccess;
        AppBusyService.SetBusy(ActionGetPurchaseOrders, false);
        return DataGridResultVM<TransferOrderDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    Task<(IEnumerable<TransferOrderStatusVM>, int)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        return ReceivingHandler.GetTransferOrderStatuses(intent);
    }

    async Task ApplyStatusFilter(RadzenDataGridColumn<TransferOrderDataGridVM> column)
    {

        column.ClearFilters();
        if (StatusFilter is null)
        {
            await TransferOrderDataGrid.DataGrid.Reload();
            await PrintStatusView.RefreshCountsAsync();
            return;
        }

        column.SetFilterOperator(FilterOperator.Equals);
        column.SetFilterValue(StatusFilter.Name);
        column.SetLogicalFilterOperator(LogicalFilterOperator.And);

        await TransferOrderDataGrid.DataGrid.Reload();
        await PrintStatusView.RefreshCountsAsync();
    }

    void ApplyStatusCountFilter(DataGridIntent intent)
    {
        if (StatusFilter is not null)
            intent.Filters.Add(DataGridFilterUtilities.Equal(nameof(TransferOrderDataGridVM.Status), StatusFilter.Name));
    }

    void ViewTransferOrder(TransferOrderDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/transfer-order/view?ref={purchaseOrder.ReferenceNumber}", true);
}
