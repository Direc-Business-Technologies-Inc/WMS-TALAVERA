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
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;

public partial class ItemSelection
{
    [Inject] IItemMasterDataHandler ItemsHandler { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required InventoryTransferRequestVM Request { get; set; } = default!;
    [Parameter] public EventCallback<InventoryTransferRequestVM> RequestChanged { get; set; } = default!;

    AppDataGrid<ItemVM> ItemsDataGrid { get; set; } = default!;
    DataGridSettings ItemsDataGridSettings { get; set; } = new();

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
            intent.Filters.Add(new AppFilterDescriptor()
            {
                LogicalOperator = LogicalOperatorEnum.AND,
                Property = "Quantity",
                ComparisonOperator = ComparisonOperatorEnum.GreaterThan,
                Value = 0,
                FilterValueType = FilterValueTypeEnum.Number
            });

            var response = await ItemsHandler.GetWarehouseItemsAsync(intent, Request.FromWarehouse?.WhsCode ?? string.Empty);
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

        if (!Request.Lines.Any(x => x.ItemCode == data.ItemCode))
        {
            InventoryTransferRequestLineVM line = new()
            {
                LineNum = Request.Lines.Count() + 1,
                ItemCode = data.ItemCode,
                ItemName = data.ItemName,
                Quantity = 0,
                OnHandQuantity = data.Quantity,
                UoMCode = data.UoMCode,
                UoMName = data.UoMName,
                UoMValue = data.UoMValue,
            };

            Request.Lines = [.. Request.Lines, line];
        }

        await RequestChanged.InvokeAsync(Request);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemVM data)
    {
        if (SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Remove(data);

        if (Request.Lines.Any(x => x.ItemCode == data.ItemCode))
            Request.Lines = [.. Request.Lines.Where(x => x.ItemCode != data.ItemCode)];

        await RequestChanged.InvokeAsync(Request);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(ItemsDataGrid.DataGrid);
    }
}
