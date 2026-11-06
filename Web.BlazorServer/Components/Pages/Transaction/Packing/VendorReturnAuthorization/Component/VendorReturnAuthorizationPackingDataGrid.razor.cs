using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.ViewModels.Transaction.Packing;
using Web.BlazorServer.Components.Pages.Transaction.Packing.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.Component;

partial class VendorReturnAuthorizationPackingDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IVendorReturnAuthorizationPackingHandler VraHandler { get; set; } = default!;
    [Inject] Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns.IReturnPackingHandler PackingHandler { get; set; } = default!;
    [Inject] ILogger<VendorReturnAuthorizationPackingDataGrid> Logger { get; set; } = default!;

    AppDataGrid<VendorReturnAuthorizationPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    PrintStatusView PrintStatusView = default!;
    bool loadCountsAfterGrid;
    bool gridReadyToLoad;
    async Task<Application.DataTransferObjects.Transactions.PrintStatusCountsDTO> PrintCountsAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var succeeded = false;
        try
        {
            var intent = new DataGridIntent { Skip = 0, Take = 1 };
            PackingFulfillmentPrintStatusFilter.ApplyToFulfillments(intent, PrintStatus.All, "VRA", CurrentUserService.NsSubsidiaryId);
            var count = await PackingHandler.GetPackingPrintStatusCounts(intent, ct);
            succeeded = true;
            return count;
        }
        finally
        {
            Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} completed in {ElapsedMs} ms; success: {Succeeded}",
                "ReturnsToSupplier", "Counts", PrintStatus.All, Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
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

    Task RefreshPrintStatusAsync() => Task.WhenAll(DataGrid.DataGrid.Reload(), PrintStatusView.RefreshCountsAsync());

    string ActionGetVendorReturnAuthorizations { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingVendorReturnAuthorizations);

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

    async Task<DataGridResultVM<VendorReturnAuthorizationPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        PackingFulfillmentPrintStatusFilter.Apply(intent, SelectedPrintStatus);
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetVendorReturnAuthorizations, true);

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
                var result = await VraHandler.GetVendorReturnAuthorizationsList(intent, CurrentUserService.NsSubsidiaryId);
                succeeded = true;
                return result;
            }
            finally
            {
                Logger.LogInformation("Packing grid {Grid} {Operation} status {PrintStatus} skip {Skip} take {Take} completed in {ElapsedMs} ms; success: {Succeeded}",
                    "ReturnsToSupplier", "Records", SelectedPrintStatus, intent.Skip, intent.Take,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds, succeeded);
            }
        }, AppActionOptionPresets.Loading(ActionGetVendorReturnAuthorizations));

        AppBusyService.SetBusy(ActionGetVendorReturnAuthorizations, false);
        return DataGridResultVM<VendorReturnAuthorizationPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewVendorReturnAuthorization(VendorReturnAuthorizationPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.VendorReturnAuthorizationView + $"?ref={item.ReferenceNumber}", true);
    }

}
