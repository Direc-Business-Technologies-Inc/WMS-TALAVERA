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
using Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReceipt.Components;

public partial class GoodsReceiptItemSelection
{
    [Inject] IItemMasterDataHandler ItemsHandler { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required GoodsReceiptVM Receipt { get; set; } = default!;
    [Parameter] public EventCallback<GoodsReceiptVM> ReceiptChanged { get; set; } = default!;

    AppDataGrid<ItemVM> ReceiptItemsDataGrid { get; set; } = default!;
    DataGridSettings ReceiptItemsDataGridSettings { get; set; } = new();

    string ActionGetItems { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllItems);

    IList<ItemVM> SelectedItems { get; set; } = [];
    List<ItemVM> Items { get; set; } = [];

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
        await GridSettingsService.SetGridSettings(ReceiptItemsDataGrid.DataGrid, settings => ReceiptItemsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await ReceiptItemsDataGrid.DataGrid.ReloadSettings();
        await ReceiptItemsDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ItemVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);

            var response = await ItemsHandler.GetWarehouseItemsAsync(intent, Receipt.Warehouse.WhsCode);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);
        return DataGridResultVM<ItemVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    async Task OnItemSelect(bool selectStatus, ItemVM data)
    {
        if (selectStatus && !SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Add(data);
        else if (!selectStatus && SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Remove(SelectedItems.First(x => x.ItemCode == data.ItemCode));
    }

    async Task OnRowSelect(ItemVM data)
    {
        if (!SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Add(data);

        if (!Receipt.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
        {
            GoodsReceiptLineVM order = new()
            {
                LineNum = Receipt.DocumentLines.Count() + 1,
                ItemCode = data.ItemCode,
                ItemName = data.ItemName,
                Quantity = 0,
                OnHandQty = data.Quantity,
                UoMCode = data.UoMCode,
                UoMName = data.UoMName,
                UoMValue = data.UoMValue,
                Warehouse = Receipt.Warehouse
            };

            Receipt.DocumentLines = [.. Receipt.DocumentLines, order];
        }

        await ReceiptChanged.InvokeAsync(Receipt);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemVM data)
    {
        if (SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Remove(data);

        if (Receipt.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
            Receipt.DocumentLines = [.. Receipt.DocumentLines.Where(x => x.ItemCode != data.ItemCode)];

        await ReceiptChanged.InvokeAsync(Receipt);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(ReceiptItemsDataGrid.DataGrid);
    }
}
