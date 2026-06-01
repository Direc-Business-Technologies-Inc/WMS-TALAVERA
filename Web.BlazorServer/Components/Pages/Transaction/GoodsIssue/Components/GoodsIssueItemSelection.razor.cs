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

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsIssue.Components;

public partial class GoodsIssueItemSelection
{
    [Inject] IItemMasterDataHandler ItemsHandler { get; set; }

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required GoodsIssueVM Issue { get; set; } = default!;
    [Parameter] public EventCallback<GoodsIssueVM> IssueChanged { get; set; } = default!;

    AppDataGrid<ItemVM> IssueItemsDataGrid { get; set; } = default!;
    DataGridSettings IssueItemsDataGridSettings { get; set; } = new();

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
        await GridSettingsService.SetGridSettings(IssueItemsDataGrid.DataGrid, settings => IssueItemsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await IssueItemsDataGrid.DataGrid.ReloadSettings();
        await IssueItemsDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ItemVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);

            var response = await ItemsHandler.GetWarehouseItemsAsync(intent, Issue.Warehouse?.WhsCode ?? string.Empty);

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

        if (!Issue.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
        {
            GoodsIssueLineVM order = new()
            {
                LineNum = Issue.DocumentLines.Count() + 1,
                ItemCode = data.ItemCode,
                ItemName = data.ItemName,
                Quantity = 0,
                OnHandQty = data.Quantity,
                UoMCode = data.UoMCode,
                UoMName = data.UoMName,
                UoMValue = data.UoMValue,
                Warehouse = Issue.Warehouse
            };

            Issue.DocumentLines = [.. Issue.DocumentLines, order];
        }

        await IssueChanged.InvokeAsync(Issue);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemVM data)
    {
        if (SelectedItems.Any(x => x.ItemCode == data.ItemCode))
            SelectedItems.Remove(data);

        if (Issue.DocumentLines.Any(x => x.ItemCode == data.ItemCode))
            Issue.DocumentLines = [.. Issue.DocumentLines.Where(x => x.ItemCode != data.ItemCode)];

        await IssueChanged.InvokeAsync(Issue);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(IssueItemsDataGrid.DataGrid);
    }
}
