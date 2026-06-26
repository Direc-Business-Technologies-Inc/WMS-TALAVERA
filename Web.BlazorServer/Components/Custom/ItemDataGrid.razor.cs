using Application.DataTransferObjects.Others;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Custom;

partial class ItemDataGrid
{

    [Parameter] public string? Id { get; set; } = "items_datagrid";
    [Parameter] public int? LocationId { get; set; }
    [Parameter] public EventCallback<List<ItemsVM>> OnItemsSelected { get; set; }
    [Parameter] public SelectionModes SelectionMode { get; set; } = SelectionModes.Single;
    [Parameter] public List<AppFilterDescriptor> Filters { get; set; } = [];
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IItemsHandler ItemsHandler { get; set; } = default!;

    private AppDataGrid<ItemsVM> DataGrid { get; set; } = default!;
    private DataGridSettings DataGridSettings = new();
    private readonly string ActionGetItems = "Get Items List";
    private List<ItemsVM> SelectedItems { get; set; } = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            if (!GridSettingsLoaded) await LoadGridSettings();
        }
    }


    async Task<DataGridResultVM<ItemsVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);

            if (Filters.Count > 0) intent.Filters.AddRange(Filters);
            var response = LocationId is null ?
                await ItemsHandler.GetItemsDataGridAsync(intent) :
                await ItemsHandler.GetItemsAtLocationDataGridAsync(intent, (int)LocationId);

            if (Filters.Count > 0) intent.Filters.AddRange(Filters);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);
        return DataGridResultVM<ItemsVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task Submit()
    {
        if (OnItemsSelected.HasDelegate)
            await OnItemsSelected.InvokeAsync(SelectedItems);
    }

    async Task SelectItem(ItemsVM item)
    {
        if (SelectionMode == SelectionModes.Single) SelectedItems.Clear();
        SelectedItems.Add(item);
        if (SelectionMode == SelectionModes.Single) await Submit();
    }

    public enum SelectionModes
    {
        Single,
        Multiple
    }
}
