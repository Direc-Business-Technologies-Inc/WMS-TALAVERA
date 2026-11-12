using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Pages.Transaction.Packing.Components;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;
using Web.BlazorServer.ViewModels.Transaction.Packing;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.STR.Component;

partial class STRPackingDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IStockTransferRequestPackingHandler StrHandler { get; set; } = default!;
    [Inject] Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns.IReturnPackingHandler PackingHandler { get; set; } = default!;
    [Inject] ILogger<STRPackingDataGrid> Logger { get; set; } = default!;

    AppDataGrid<StockTransferRequestPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    PrintStatusView PrintStatusView = default!;
    bool initialCountRefreshRequested;
    bool gridReadyToLoad;
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    bool dateRangeApplied;

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        dateRangeApplied = true;
        DateRangeStart = range.Start;
        DateRangeEnd = range.End;
        await DataGrid.ReloadFirstPageAsync();
    }

    Task OnGridDataLoaded()
    {
        if (!dateRangeApplied)
        {
            // Display the visible page's dates without narrowing future reads.
            var hasDates = PrintStatusFilter.TryGetDateRange(DataGrid.DGResult.Items, row => row.Date, out var range);
            DateRangeStart = hasDates ? range.Start : null;
            DateRangeEnd = hasDates ? range.End : null;
        }
        if (initialCountRefreshRequested) return Task.CompletedTask;
        initialCountRefreshRequested = true;
        _ = InvokeAsync(async () => { await Task.Yield(); await PrintStatusView.RefreshCountsAsync(); });
        return Task.CompletedTask;
    }

    async Task<Application.DataTransferObjects.Transactions.PrintStatusCountsDTO> PrintCountsAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var succeeded = false;
        try
        {
            var intent = new DataGridIntent { Skip = 0, Take = 1 };
            PackingFulfillmentPrintStatusFilter.ApplyToFulfillments(intent, PrintStatus.All, "STR", CurrentUserService.NsSubsidiaryId);
            var count = await PackingHandler.GetPackingPrintStatusCounts(intent, ct);
            succeeded = true;
            return count;
        }
        finally
        {
            Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} completed in {ElapsedMs} ms; success: {Succeeded}",
                "STR", "Counts", PrintStatus.All, Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
        }
    }

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        if (DataGrid.DataGrid.CurrentPage == 0)
            await DataGrid.DataGrid.Reload();
        else
            await DataGrid.DataGrid.GoToPage(0);
    }

    Task RefreshPrintStatusAsync() => Task.WhenAll(DataGrid.RefreshOrThrowAsync(), PrintStatusView.RefreshCountsOrThrowAsync());
    Task SyncAsync() => Task.WhenAll(DataGrid.ReloadDataAsync(), PrintStatusView.RefreshCountsAsync());

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
        // Publish settings, visibility and the getter together. The shared grid
        // applies settings after rendering, then performs one initial read.
        GridSettingsLoaded = true;
        gridReadyToLoad = true;
    }

    void PrepareIntent(DataGridIntent intent)
    {
        PackingFulfillmentPrintStatusFilter.Apply(intent, SelectedPrintStatus);
        if (dateRangeApplied)
            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
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

                intent.Sorts.Add(new()
                {
                    Property = "ReferenceNumber",
                    Direction = SortDirectionEnum.Descending
                });
            }

            var started = Stopwatch.GetTimestamp();
            var succeeded = false;
            try
            {
                var result = await StrHandler.GetStockTransferRequestsList(
                    intent, CurrentUserService.NsSubsidiaryId, includeReceived: true);
                succeeded = true;
                return result;
            }
            finally
            {
                Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} skip {Skip} take {Take} completed in {ElapsedMs} ms; success: {Succeeded}",
                    "STR", "Records", SelectedPrintStatus, intent.Skip, intent.Take,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
            }
        }, AppActionOptionPresets.Loading(ActionGetStockTransferRequests));

        AppBusyService.SetBusy(ActionGetStockTransferRequests, false);
        action.ThrowIfUnsuccessful();
        return DataGridResultVM<StockTransferRequestPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSTR(StockTransferRequestPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.StockTransferRequestView + $"?ref={item.ReferenceNumber}", true);
    }

}
