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

    async Task<DataGridResultVM<TransferOrderLineVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            return await ReceivingHandler.GetTransferOrderLinesDataGridAsync(Ref, intent);
        }, AppActionOptionPresets.Loading(ActionGetLines));

        AppBusyService.SetBusy(ActionGetLines, false);
        await InvokeAsync(StateHasChanged);

        return DataGridResultVM<TransferOrderLineVM>.New(action.Result.Data ?? [], action.Result.Count);
    }
}
