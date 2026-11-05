using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.Component;

partial class ReturnsPackingDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IReturnPackingHandler ReturnsHandler { get; set; } = default!;
    [Inject] ILogger<ReturnsPackingDataGrid> Logger { get; set; } = default!;

    AppDataGrid<ReturnsPackingDataGridVM> DataGrid { get; set; } = default!;
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
            PackingFulfillmentPrintStatusFilter.ApplyToFulfillments(intent, status, "RETURNS", CurrentUserService.NsSubsidiaryId);
            var (_, count) = await ReturnsHandler.GetPackedItemFulfillments(intent);
            succeeded = true;
            return count;
        }
        finally
        {
            Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} completed in {ElapsedMs} ms; success: {Succeeded}",
                "Returns", "Count", status, Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
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

    string ActionGetReturns { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingReturns);

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

    async Task<DataGridResultVM<ReturnsPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        PackingFulfillmentPrintStatusFilter.Apply(intent, SelectedPrintStatus);
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetReturns, true);

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
                var result = await ReturnsHandler.GetReturnsList(intent, CurrentUserService.NsSubsidiaryId);
                succeeded = true;
                return result;
            }
            finally
            {
                Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} skip {Skip} take {Take} completed in {ElapsedMs} ms; success: {Succeeded}",
                    "Returns", "Records", SelectedPrintStatus, intent.Skip, intent.Take,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
            }
        }, AppActionOptionPresets.Loading(ActionGetReturns));

        AppBusyService.SetBusy(ActionGetReturns, false);
        return DataGridResultVM<ReturnsPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewReturn(ReturnsPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.ReturnsView + $"?ref={item.ReferenceNumber}", true);
    }
}
