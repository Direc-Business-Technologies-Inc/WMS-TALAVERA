using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.Component;

partial class ReturnsPackingDetails
{
    [Parameter]
    [EditorRequired]
    public string? Ref { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IReturnPackingHandler ReturnsHandler { get; set; } = default!;

    bool IsDataLoaded = false;
    bool ErrorState = false;

    string ActionGetLines => "Get Packing Return Lines";
    string ActionGetInfo => "Get Packing Return Information";
    string ErrorMessage = "Something went wrong while loading the packing return details. Please try again.";

    ReturnsInfoPackingVM Model = new();
    AppDataGrid<ReturnsLinePackingVM>? DataGrid { get; set; }
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
            if (string.IsNullOrWhiteSpace(Ref)) throw new Exception("Packing return reference is required");

            var result = await ReturnsHandler.GetPackingReturn(Ref);
            if (result is null) throw new Exception("Packing return does not exist");

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
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=returns");
            await Task.CompletedTask;
        });
    }

    async Task<DataGridResultVM<ReturnsLinePackingVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(Ref)) throw new Exception("Packing return reference is required");

            return await ReturnsHandler.GetPackingReturnLines(Ref, intent);
        }, AppActionOptionPresets.Loading(ActionGetLines));

        AppBusyService.SetBusy(ActionGetLines, false);
        await InvokeAsync(StateHasChanged);

        return DataGridResultVM<ReturnsLinePackingVM>.New(action.Result.Data ?? [], action.Result.Count);
    }
}
