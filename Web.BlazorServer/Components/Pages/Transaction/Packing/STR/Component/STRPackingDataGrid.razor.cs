using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Custom;
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
    [Inject] Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns.IReturnPackingHandler PackingHandler { get; set; } = default!;
    [Inject] ILogger<STRPackingDataGrid> Logger { get; set; } = default!;

    AppDataGrid<StockTransferRequestPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    PrintStatusView PrintStatusView = default!;
    bool loadCountsAfterGrid;
    bool gridReadyToLoad;

    async Task<int> PrintCountAsync(PrintStatus status)
    {
        var started = Stopwatch.GetTimestamp();
        var succeeded = false;
        try
        {
            var intent = new DataGridIntent { Skip = 0, Take = 1 };
            PackingFulfillmentPrintStatusFilter.ApplyToFulfillments(intent, status, "STR", CurrentUserService.NsSubsidiaryId);
            var (_, count) = await PackingHandler.GetPackedItemFulfillments(intent);
            succeeded = true;
            return count;
        }
        finally
        {
            Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} completed in {ElapsedMs} ms; success: {Succeeded}",
                "STR", "Count", status, Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
        }
    }

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        if (DataGrid.DataGrid.CurrentPage == 0)
            await DataGrid.DataGrid.Reload();
        else
            await DataGrid.DataGrid.GoToPage(0);
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task RefreshPrintStatusAsync()
    {
        await DataGrid.DataGrid.Reload();
        await PrintStatusView.RefreshCountsAsync();
    }

    string ActionGetStockTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingStockTransferRequest);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
        if (loadCountsAfterGrid && PrintStatusView is not null)
        {
            loadCountsAfterGrid = false;
            _ = InvokeAsync(PrintStatusView.RefreshCountsAsync);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        gridReadyToLoad = true;
        await InvokeAsync(StateHasChanged);

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
        GridSettingsLoaded = true;
        loadCountsAfterGrid = true;
    }

    async Task<DataGridResultVM<StockTransferRequestPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        PackingFulfillmentPrintStatusFilter.Apply(intent, SelectedPrintStatus);
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
                var result = await StrHandler.GetStockTransferRequestsList(intent, CurrentUserService.NsSubsidiaryId);
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
        return DataGridResultVM<StockTransferRequestPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewSTR(StockTransferRequestPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.StockTransferRequestView + $"?ref={item.ReferenceNumber}", true);
    }
}
