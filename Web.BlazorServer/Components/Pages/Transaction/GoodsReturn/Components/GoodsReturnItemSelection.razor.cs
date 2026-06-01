    using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReturn;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReturn.Components;

public partial class GoodsReturnItemSelection : IAsyncDisposable
{
    [Inject] IItemMasterDataHandler ItemsHandler { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required GoodsReturnVM Return { get; set; } = default!;
    [Parameter] public EventCallback<GoodsReturnVM> ReturnChanged { get; set; } = default!;

    AppDataGrid<ItemVM> ReturnItemsDataGrid { get; set; } = default!;
    DataGridSettings ReturnItemsDataGridSettings { get; set; } = new();

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
        await GridSettingsService.SetGridSettings(ReturnItemsDataGrid.DataGrid, settings => ReturnItemsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await ReturnItemsDataGrid.DataGrid.ReloadSettings();
        await ReturnItemsDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ItemVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);

            var response = await ItemsHandler.GetMerchandiseItemsAsync(intent);

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

        if (!Return.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
        {
            GoodsReturnLineVM order = new()
            {
                LineNum = Return.DocumentLines.Count + 1,
                ItemCode = data.ItemCode,
                ItemName = data.ItemName,
                Quantity = 0,
                UoMCode = data.UoMCode,
                UoMName = data.UoMName,
                UoMValue = data.UoMValue,
                Warehouse = Return.Warehouse
            };

            Return.DocumentLines.Add(order);
        }

        await ReturnChanged.InvokeAsync(Return);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemVM data)
    {
        if (SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Remove(data);

        if (Return.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
            Return.DocumentLines.Remove(Return.DocumentLines.First(x => x.ItemCode == data.ItemCode));

        await ReturnChanged.InvokeAsync(Return);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(ReturnItemsDataGrid.DataGrid);
    }
}
