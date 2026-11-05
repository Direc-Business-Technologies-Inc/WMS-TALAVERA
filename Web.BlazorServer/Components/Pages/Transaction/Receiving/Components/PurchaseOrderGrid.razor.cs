using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Filters;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class PurchaseOrderGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    AppDataGrid<PurchaseOrderDataGridVM> PurchaseOrderDataGrid { get; set; }
    DataGridSettings PurchaseOrderDataGridSettings { get; set; }

    PurchaseOrderStatusVM? StatusFilter = null;
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
        ReceivingReceiptPrintStatusFilter.ApplyToReceipts(intent, status, "PO");
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
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task ReloadFirstPageAsync()
    {
        if (PurchaseOrderDataGrid.DataGrid.CurrentPage == 0)
            await PurchaseOrderDataGrid.DataGrid.Reload();
        else
            await PurchaseOrderDataGrid.DataGrid.GoToPage(0);
    }

    async Task RefreshPrintStatusAsync()
    {
        await ReloadFirstPageAsync();
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        if (PurchaseOrderDataGrid.DataGrid.CurrentPage == 0)
            await PurchaseOrderDataGrid.DataGrid.Reload();
        else
            await PurchaseOrderDataGrid.DataGrid.GoToPage(0);
    }

    string ActionGetPurchaseOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPurchaseOrders);

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
        await GridSettingsService.SetGridSettings(PurchaseOrderDataGrid.DataGrid, settings => PurchaseOrderDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await PurchaseOrderDataGrid.DataGrid.ReloadSettings();
        await PurchaseOrderDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<PurchaseOrderDataGridVM>> LoadDataAsync(DataGridIntent intent)
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

            var result = await ReceivingHandler.GetPurchaseOrderDataGridAsync(intent);
            return result;

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrders));

        initialGridLoadSucceeded = action.IsSuccess;
        AppBusyService.SetBusy(ActionGetPurchaseOrders, false);
        return DataGridResultVM<PurchaseOrderDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    Task<(IEnumerable<PurchaseOrderStatusVM>, int)> GetPurchaseOrderStatuses(DataGridIntent intent)
    {
        return ReceivingHandler.GetPurchaseOrderStatuses(intent);
    }

    async Task ApplyStatusFilter(RadzenDataGridColumn<PurchaseOrderDataGridVM> column)
    {

        column.ClearFilters();
        if (StatusFilter is null)
        {
            await PurchaseOrderDataGrid.DataGrid.Reload();
            await PrintStatusView.RefreshCountsAsync();
            return;
        }

        column.SetFilterOperator(FilterOperator.Equals);
        column.SetFilterValue(StatusFilter.Name);
        column.SetLogicalFilterOperator(LogicalFilterOperator.And);

        await PurchaseOrderDataGrid.DataGrid.Reload();
        await PrintStatusView.RefreshCountsAsync();
    }

    void ApplyStatusCountFilter(DataGridIntent intent)
    {
        if (StatusFilter is not null)
            intent.Filters.Add(DataGridFilterUtilities.Equal(nameof(PurchaseOrderDataGridVM.Status), StatusFilter.Name));
    }

    void ViewPurchaseOrder(PurchaseOrderDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/view?ref={purchaseOrder.ReferenceNumber}", true);
}
