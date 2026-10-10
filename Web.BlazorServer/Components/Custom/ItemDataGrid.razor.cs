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

partial class ItemDataGrid : IAsyncDisposable
{

    [Parameter] public string? Id { get; set; } = "items_datagrid";
    [Parameter] public int? LocationId { get; set; }
    [Parameter] public int? SubsidiaryId { get; set; }
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
    private CancellationTokenSource? _loadCts;
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
        // A new load supersedes any in-flight one; disposing the component
        // (navigation away) cancels the current load so the underlying
        // NetSuite HTTP request is aborted (Req 3.1).
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        DataGridResultVM<ItemsVM> cancelled() =>
            DataGridResultVM<ItemsVM>.New(Enumerable.Empty<ItemsVM>(), 0);

        try
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
                ? await ItemsHandler.GetItemsDataGridAsync(intent, ct)
                : await ItemsHandler.GetItemsAtLocationDataGridAsync(
                    intent,
                    (int)LocationId,
                    SubsidiaryId,
                    ct);

            ct.ThrowIfCancellationRequested();

            return response;

        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);

        // Cancelled/failed loads resolve silently with empty data (Req 3.3, 5.2):
        // the factory marks cancellations with no error UI.
        if (!action.IsSuccess)
            return cancelled();

        return DataGridResultVM<ItemsVM>.New(
            action.Result.Data ?? [],
            action.Result.Count);
    }
    catch (OperationCanceledException)
    {
        return cancelled();
    }
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

    // "All" refers to the rows of the currently loaded page only; paging
    // through the whole data set is intentionally not triggered from here.
    List<ItemsVM> PageItems => DataGrid?.DGResult.Items?.ToList() ?? [];

    bool? GetSelectionHeaderState()
    {
        var pageItems = PageItems;
        return SelectAllHeader.Compute(
            pageItems.Count,
            pageItems.Count(item => SelectedItems.Any(x => x.ItemNumber == item.ItemNumber)));
    }

    async Task ToggleAllItems(bool? value)
    {
        // Selecting the whole page only makes sense for multi-select grids.
        if (SelectionMode != SelectionModes.Multiple)
            return;

        bool select = value == true;

        foreach (var item in PageItems)
        {
            bool isSelected = SelectedItems.Any(x => x.ItemNumber == item.ItemNumber);

            // SelectItem toggles in Multiple mode, so only touch items whose
            // state actually differs from the requested one.
            if (select != isSelected)
                await SelectItem(item);
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }

    public enum SelectionModes
    {
        Single,
        Multiple
    }
}
