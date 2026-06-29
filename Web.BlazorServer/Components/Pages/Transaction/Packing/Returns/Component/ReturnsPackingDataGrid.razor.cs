using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Base;
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

    AppDataGrid<ReturnsPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();

    string ActionGetReturns { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingReturns);

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

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ReturnsPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetReturns, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = nameof(ReturnsPackingDataGridVM.Date),
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await ReturnsHandler.GetReturnsList(intent);
        }, AppActionOptionPresets.Loading(ActionGetReturns));

        AppBusyService.SetBusy(ActionGetReturns, false);
        return DataGridResultVM<ReturnsPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewReturn(ReturnsPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.ReturnsView + $"?ref={item.ReferenceNumber}", true);
    }
}
