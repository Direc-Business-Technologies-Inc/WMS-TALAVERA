using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor.Markdown;
using System.ComponentModel.DataAnnotations;
using System.Timers;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransfer;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;

partial class ReadOnlyForm
{
    #region Parameters
    [Parameter] public List<NavigationRouteVM>? AdditionalRoutes { get; set; }
    [Parameter, EditorRequired] public required string ActionName { get; set; }
    [Parameter, EditorRequired] public required string ParentURI { get; set; }
    [Parameter, EditorRequired] public required Func<Task<InventoryTransferCVUVM>> DataGetter { get; set; }
    [Parameter] public RenderFragment ContentBadges { get; set; }

    #endregion

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    AppTable<InventoryTransferCVULineVM> LinesTable { get; set; } = default!;
    DataGridSettings LinesTableSettings { get; set; } = new();
    bool IsBusy => AppBusyService.IsBusy(ActionName);
    bool IsLoadingData => AppBusyService.IsBusy(ActionName);

    #region Function Overrides
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }
    #endregion

    #region Custom Functions
    async Task LoadGridSettings()
    {
        try
        {
            await GridSettingsService.SetGridSettings(
                LinesTable.DataGrid,
                settings => LinesTableSettings = settings ?? new()
            );

            GridSettingsLoaded = true;

            await LinesTable.DataGrid.ReloadSettings();
            await LinesTable.DataGrid.Reload();
        }
        catch (Exception ex) {
            ToastService.Warning(ex.Message, "An error happened while loading the settings");
        }
    }
    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionName))
        {
            AppBusyService.SetBusy(ActionName, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await LoadData();

        AppBusyService.SetBusy(ActionName, false);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
        AppBusyService.SetBusy(ActionName, false);
    }
    async Task CancelButtonClicked()
    {
        NavManager.NavigateTo(ParentURI);
    }
    async Task LoadData()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            var result = await DataGetter();
            AppBusyService.SetBusy(ActionName, false);
            return result;
        }, AppActionOptionPresets.Loading(ActionName));

        action.OnSuccess(async (args) =>
        {
            action.Result.Adapt(FormData);
        });

        action.OnFailure(async (args) =>
        {
            NavManager.NavigateTo(ParentURI);
        });
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }
    #endregion
}