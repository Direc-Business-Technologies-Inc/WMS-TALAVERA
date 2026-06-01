using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;

public partial class InventoryCountingItemSelection
{
    [Inject] IItemMasterDataHandler ItemsHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required InventoryCountingVM Document { get; set; }
    [Parameter] public EventCallback<InventoryCountingVM> DocumentChanged { get; set; }

    AppDataGrid<ItemVM> ItemsDataGrid { get; set; } = default!;
    DataGridSettings ItemsDataGridSettings { get; set; } = new();

    string ActionGetItems { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllItems);

    IList<ItemVM> SelectedItems { get; set; } = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
        }
    }

    async Task LoadGridSettings()
    {
        // Pre-select items already in the document
        SelectedItems = [.. Document.DocumentLines.Select(dl => new ItemVM
        {
            ItemCode = dl.ItemCode,
            ItemName = dl.ItemName,
            Quantity = dl.Quantity,
            UoMCode = dl.UoMCode,
            UoMValue = dl.UoMValue,
            UoMName = dl.UoMName,
        })];

        await GridSettingsService.SetGridSettings(ItemsDataGrid.DataGrid, settings => ItemsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;
        await ItemsDataGrid.DataGrid.ReloadSettings();
        await ItemsDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ItemVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);
            return await ItemsHandler.GetWarehouseItemsAsync(intent, Document.Warehouse?.WhsCode ?? string.Empty);
        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);
        return DataGridResultVM<ItemVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    async Task OnRowSelect(ItemVM data)
    {
        if (!SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Add(data);

        if (!Document.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
        {
            var line = new InventoryCountingLineVM
            {
                ItemCode = data.ItemCode,
                ItemName = data.ItemName,
                Quantity = data.Quantity,
                UoMCode = data.UoMCode,
                UoMValue = data.UoMValue,
                UoMName = data.UoMName,
            };
            Document.DocumentLines = [.. Document.DocumentLines, line];
        }

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemVM data)
    {
        var existing = SelectedItems.FirstOrDefault(x => x.ItemCode == data.ItemCode);
        if (existing is not null) SelectedItems.Remove(existing);

        if (Document.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
            Document.DocumentLines = [.. Document.DocumentLines.Where(x => x.ItemCode != data.ItemCode)];

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(ItemsDataGrid.DataGrid);
    }
}
