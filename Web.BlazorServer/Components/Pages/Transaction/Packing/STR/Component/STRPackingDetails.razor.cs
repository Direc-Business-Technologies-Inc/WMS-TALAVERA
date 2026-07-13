using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.STR.Component;

partial class STRPackingDetails
{
    [Parameter]
    [EditorRequired]
    public string? Ref { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IStockTransferRequestPackingHandler StrHandler { get; set; } = default!;

    bool IsDataLoaded = false;
    bool ErrorState = false;

    string ActionGetLines => "Get Packing Stock Transfer Request Lines";
    string ActionGetInfo => "Get Packing Stock Transfer Request Information";
    string ErrorMessage = "Something went wrong while loading the packing stock transfer request details. Please try again.";
    const string PRINTABLE_URL = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1922&deploy=1&compid=11608969&ns-at=AAEJ7tMQ70cbDMgsewbx6YHr0oQkl5HAZi1-qpSrLgdV9mevdZI";

    StockTransferRequestInfoPackingVM Model = new();
    AppDataGrid<StockTransferRequestLinePackingVM>? DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; } = new();

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
        if (DataGrid is null) return;

        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task LoadHeader()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(Ref)) throw new Exception("Packing stock transfer request reference is required");

            var result = await StrHandler.GetPackingStockTransferRequest(Ref);
            if (result is null) throw new Exception("Packing stock transfer request does not exist");

            return result;
        }, AppActionOptionPresets.Loading(ActionGetInfo));

        action.OnSuccess(async result =>
        {
            Model = result;
            IsDataLoaded = true;
            await InvokeAsync(StateHasChanged);
            await LoadGridSettings();
        });

        action.OnFailure(async ex =>
        {
            ErrorState = true;
            ErrorMessage = ex.Message;
            ToastService.Error(ex.Message);
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=stocktransferrequest");
            await Task.CompletedTask;
        });
    }

    string PrintableURL => $"{PRINTABLE_URL}&id={Model.Id}";

    async Task<DataGridResultVM<StockTransferRequestLinePackingVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(Ref)) throw new Exception("Packing stock transfer request reference is required");

            return await StrHandler.GetPackingStockTransferRequestLines(Ref, intent);
        }, AppActionOptionPresets.Loading(ActionGetLines));

        AppBusyService.SetBusy(ActionGetLines, false);
        await InvokeAsync(StateHasChanged);

        return DataGridResultVM<StockTransferRequestLinePackingVM>.New(action.Result.Data ?? [], action.Result.Count);
    }
}
