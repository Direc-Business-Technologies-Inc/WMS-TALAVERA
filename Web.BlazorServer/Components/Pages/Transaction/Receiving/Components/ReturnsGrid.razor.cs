using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class ReturnsGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    AppDataGrid<ReturnsDataGridVM> DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }

    string ActionGetPurchaseOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllReturns);

    TransferOrderStatusVM? StatusFilter = null;
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    bool dateRangeApplied;
    PrintStatusView PrintStatusView = default!;
    bool initialGridLoadSucceeded;
    bool initialCountRefreshRequested;

    Task OnGridDataLoaded()
    {
        if (!dateRangeApplied)
        {
            var hasDates = PrintStatusFilter.TryGetDateRange(DataGrid.DGResult.Items, row => row.Date, out var range);
            DateRangeStart = hasDates ? range.Start : null;
            DateRangeEnd = hasDates ? range.End : null;
        }
        if (!initialGridLoadSucceeded || initialCountRefreshRequested) return Task.CompletedTask;
        initialCountRefreshRequested = true;
        _ = InvokeAsync(async () => { await Task.Yield(); await PrintStatusView.RefreshCountsAsync(); });
        return Task.CompletedTask;
    }

    async Task<Application.DataTransferObjects.Transactions.PrintStatusCountsDTO> PrintCountsAsync(CancellationToken ct)
    {
        var intent = new DataGridIntent { Skip = 0, Take = 1, ReadPolicy = GridReadPolicy.Fresh, CancellationToken = ct };
        ReceivingReceiptPrintStatusFilter.ApplyToReceipts(intent, PrintStatus.All, "RETURNS");
        intent.Filters.Add(DataGridFilterUtilities.Equal("IncludeCompletedReceived", true));
        if (dateRangeApplied)
            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd, "tcf.trandate");
        ApplyStatusCountFilter(intent);
        return await ReceivingHandler.GetReceivingPrintStatusCounts(intent, ct);
    }

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        dateRangeApplied = true;
        DateRangeStart = range.Start;
        DateRangeEnd = range.End;
        if (DataGrid.DataGrid.CurrentPage == 0)
            await DataGrid.DataGrid.Reload();
        else
            await DataGrid.DataGrid.GoToPage(0);
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        if (DataGrid.DataGrid.CurrentPage == 0)
            await DataGrid.DataGrid.Reload();
        else
            await DataGrid.DataGrid.GoToPage(0);
    }

    Task ReloadFirstPageAsync() => Task.WhenAll(DataGrid.RefreshFirstPageOrThrowAsync(), PrintStatusView.RefreshCountsOrThrowAsync());
    Task SyncAsync() => Task.WhenAll(DataGrid.ReloadFirstPageAsync(), PrintStatusView.RefreshCountsAsync());

    async Task ReloadRowsAsync()
    {
        if (DataGrid.DataGrid.CurrentPage == 0)
            await DataGrid.DataGrid.Reload();
        else
            await DataGrid.DataGrid.GoToPage(0);
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
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;
    }

    void PrepareIntent(DataGridIntent intent)
    {
        ReceivingReceiptPrintStatusFilter.ApplyToTransactions(intent, SelectedPrintStatus, "RETURNS");
        intent.Filters.Add(DataGridFilterUtilities.Equal("IncludeCompletedReceived", true));
        if (dateRangeApplied)
            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
    }

    async Task<DataGridResultVM<ReturnsDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
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
            var result = await ReceivingHandler.GetReturnsDataGridAsync(intent);
            return result;

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrders));

        initialGridLoadSucceeded = action.IsSuccess;
        AppBusyService.SetBusy(ActionGetPurchaseOrders, false);
        action.ThrowIfUnsuccessful();
        return DataGridResultVM<ReturnsDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    Task<(IEnumerable<TransferOrderStatusVM>, int)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        return ReceivingHandler.GetTransferOrderStatuses(intent);
    }

    async Task ApplyStatusFilter(RadzenDataGridColumn<ReturnsDataGridVM> column)
    {

        column.ClearFilters();
        if (StatusFilter is null)
        {
            await DataGrid.DataGrid.Reload();
            await PrintStatusView.RefreshCountsAsync();
            return;
        }

        column.SetFilterOperator(FilterOperator.Equals);
        column.SetFilterValue(StatusFilter.Name);
        column.SetLogicalFilterOperator(LogicalFilterOperator.And);

        await DataGrid.DataGrid.Reload();
        await PrintStatusView.RefreshCountsAsync();
    }

    void ApplyStatusCountFilter(DataGridIntent intent)
    {
        if (StatusFilter is not null)
            intent.Filters.Add(DataGridFilterUtilities.Equal(nameof(ReturnsDataGridVM.Status), StatusFilter.Name));
    }

    void ViewItem(ReturnsDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/returns/view?ref={purchaseOrder.ReferenceNumber}", true);
}
