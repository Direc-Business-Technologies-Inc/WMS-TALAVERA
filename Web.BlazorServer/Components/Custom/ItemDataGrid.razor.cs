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
    private string? SearchText { get; set; }
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

            var filters = new List<AppFilterDescriptor>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchFilter = new AppFilterDescriptor
                {
                    LogicalOperator = LogicalOperatorEnum.OR,
                    Filters =
                    [
                        new AppFilterDescriptor
                    {
                        Property = nameof(ItemsVM.ItemNumber),
                        Value = SearchText,
                        FilterValueType = FilterValueTypeEnum.String,
                        ComparisonOperator = ComparisonOperatorEnum.Contains
                    },
                    new AppFilterDescriptor
                    {
                        Property = nameof(ItemsVM.Name),
                        Value = SearchText,
                        FilterValueType = FilterValueTypeEnum.String,
                        ComparisonOperator = ComparisonOperatorEnum.Contains
                    },
                    new AppFilterDescriptor
                    {
                        Property = nameof(ItemsVM.Description),
                        Value = SearchText,
                        FilterValueType = FilterValueTypeEnum.String,
                        ComparisonOperator = ComparisonOperatorEnum.Contains
                    }
                    ]
                };

                filters.Add(searchFilter);
            }

            if (Filters.Count > 0)
            {
                filters.AddRange(Filters);
            }

            intent.Filters = filters;

            var response = LocationId is null
                ? await ItemsHandler.GetItemsDataGridAsync(intent)
                : await ItemsHandler.GetItemsAtLocationDataGridAsync(
                    intent,
                    (int)LocationId);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);

        return DataGridResultVM<ItemsVM>.New(
            action.Result.Data ?? [],
            action.Result.Count);
    }

    async Task OnSearchChanged(object? value)
    {
        SearchText = value?.ToString();

        await DataGrid.ReloadDataAsync();
    }

    async Task ClearSearch()
    {
        SearchText = null;

        await DataGrid.ReloadDataAsync();
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
        var selectedItem = SelectedItems.FirstOrDefault(x => x.ItemNumber == item.ItemNumber);

        if (SelectionMode == SelectionModes.Single)
        {
            SelectedItems.Clear();
            SelectedItems.Add(item);
            await Submit();
        }
        else if (SelectionMode == SelectionModes.Multiple)
        {
            if (selectedItem is not null)
            {
                SelectedItems.Remove(selectedItem);
            }
            else
            {
                SelectedItems.Add(item);
            }
        }
    }

    public enum SelectionModes
    {
        Single,
        Multiple
    }
}
