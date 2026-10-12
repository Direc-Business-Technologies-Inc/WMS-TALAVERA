using Application.DataTransferObjects.Others.NS;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class TransferOrderDetails
{
    [Parameter]
    [EditorRequired]
    public string Ref { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetInfo);
    bool IsBusy => AppBusyService.IsBusy(ActionGetLines);
    bool IsDataLoaded = false;
    bool ErrorState = false;

    string ActionGetLines => $"Get Transfer Order Lines";
    string ActionGetInfo => $"Get Transfer Order Information";
    string ErrorMessage = "Something went wrong while loading the transfer order details. Please try again.";

    TransferOrderVM Model = new();
    AppDataGrid<TransferOrderLineVM>? DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await LoadHeader();
        }
    }


    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task LoadHeader()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrEmpty(Ref)) throw new Exception("Transfer Order ID is required");
            var result = await ReceivingHandler.GetTransferOrderAsync(Ref);
            if (result is null) throw new Exception("Transfer Order does not exist");
            return result;
        }, AppActionOptionPresets.Loading(ActionGetLines));

        action.OnSuccess(async (result) =>
        {
            Model = result;
            IsDataLoaded = true;
            await InvokeAsync(StateHasChanged);
            await LoadGridSettings();
        });

        action.OnFailure(async (ex) =>
        {
            NavManager.NavigateTo("/transactions/purchasing/receiving?tab=transferorder");
        });
    }

    // Last good page, kept so a cancelled (superseded, navigation-away) load
    // doesn't wipe the grid to "No records to display".
    DataGridResultVM<TransferOrderLineVM> LastResult = DataGridResultVM<TransferOrderLineVM>.New([], 0);

    async Task<DataGridResultVM<TransferOrderLineVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            return await ReceivingHandler.GetTransferOrderLinesDataGridAsync(Ref, intent);
        }, AppActionOptionPresets.Loading(ActionGetLines));

        await InvokeAsync(StateHasChanged);

        // AppActionFactory swallows the exception and returns normally, so a failed
        // NetSuite call leaves Result unset. Dereferencing it unconditionally would
        // throw from inside Radzen's DataGetter, masking the real NetSuite error
        // behind a second, unrelated exception.
        //
        // Order matters: check the offset-corrected recovery first (it arrives as
        // a failure-shaped action carrying good rows), then cancellation (keep the
        // previous page, stay silent), then genuine failure (empty, real toast
        // already shown by the factory).
        if (action.Exception is NetSuiteOffsetCorrectedException<IEnumerable<TransferOrderLineVM>> corrected)
        {
            LastResult = DataGridResultVM<TransferOrderLineVM>.New(
                corrected.Rows ?? [], corrected.TotalResults);

            // Reposition the pager to the page the integration actually served.
            // intent.Take is the live page size Radzen just asked for, so this
            // stays correct even when the user changed the page size mid-flight.
            // The page is only moved when it differs; when already on the right
            // page nothing further happens.
            if (DataGrid?.DataGrid is not null && intent.Take > 0)
            {
                var page = Math.Max(0, corrected.CorrectedSkip / intent.Take);
                if (page != DataGrid.DataGrid.CurrentPage)
                {
                    await DataGrid.DataGrid.GoToPage(page);
                }
            }

            return LastResult;
        }

        if (action.IsCancelled)
        {
            return LastResult;
        }

        if (!action.IsSuccess)
        {
            return DataGridResultVM<TransferOrderLineVM>.New([], 0);
        }

        LastResult = DataGridResultVM<TransferOrderLineVM>.New(action.Result.Data ?? [], action.Result.Count);
        return LastResult;
    }
}
