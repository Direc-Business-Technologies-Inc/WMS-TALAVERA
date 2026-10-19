using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using System.Reflection;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppDataGrid<TItem> : BaseComponent where TItem : class
{
    #region Injects
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Parameter

    [Parameter] public string Id { get; init; } = $"{typeof(TItem).Name}-data-grid";
    [Parameter] public List<TItem> Data { get; set; } = new List<TItem>();
    [Parameter] public EventCallback<List<TItem>> DataChanged { get; set; }
    [Parameter] public Func<DataGridIntent, Task<DataGridResultVM<TItem>>>? DataGetter { get; set; }
    [Parameter] public RenderFragment HeaderTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    [Parameter] public RenderFragment Footer { get; set; }
    [Parameter] public RenderFragment LoadingTemplate { get; set; }
    [Parameter] public RenderFragment DetailTemplate { get; set; }
    [Parameter] public RenderFragment EmptyTemplate { get; set; }
    [Parameter] public RenderFragment GridColumns { get; set; }
    [Parameter] public bool ServerSide { get; set; } = true;
    [Parameter] public DataGridSettings GridSettings { get; set; } = new();
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; set; }
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowClick { get; set; }
    [Parameter] public EventCallback<TItem> OnRowSelect { get; set; }
    [Parameter] public EventCallback<TItem> OnRowDeselect { get; set; }
    [Parameter] public DataGridSelectionMode DataGridSelectionMode { get; set; } = DataGridSelectionMode.Single;
    [Parameter] public bool GridSettingsLoaded { get; set; } = false;
    [Parameter] public EventCallback<bool> GridSettingsLoadedChanged { get; set; }
    [Parameter] public IList<TItem> SelectedItems { get; set; } = new List<TItem>();
    [Parameter] public EventCallback<IList<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public string ActionName { get; set; } = string.Empty;
    [Parameter] public bool ClientSide { get; set; }
    [Parameter] public IEnumerable<AppFilterDescriptor> DefaultFilters { get; set; } = [];
    #endregion Parameter

    public RadzenDataGrid<TItem>? DataGrid { get; set; } = default!;
    public DataGridResultVM<TItem> DGResult { get; set; } = new DataGridResultVM<TItem>();
    public EventCallback<DataGridResultVM<TItem>> DGResultChanged { get; set; }
    public IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    protected bool IsBusy => AppBusyService.IsBusy(ActionName);

    #region Selection identity

    // Server-side loads materialize a brand new instance per row, so selection
    // has to be re-identified by a business key rather than by reference.
    private static readonly string[] SelectionKeyPropertyNames = ["ItemCode", "ItemNumber", "OrderNumber", "Id"];
    private static PropertyInfo? _selectionKeyProperty;
    private static bool _selectionKeyResolved;

    private static string? SelectionKey(TItem item)
    {
        if (item is null)
            return null;

        if (!_selectionKeyResolved)
        {
            _selectionKeyProperty = SelectionKeyPropertyNames
                .Select(name => typeof(TItem).GetProperty(name, BindingFlags.Public | BindingFlags.Instance))
                .FirstOrDefault(prop => prop is not null);

            _selectionKeyResolved = true;
        }

        if (_selectionKeyProperty is null)
            return null;

        var value = _selectionKeyProperty.GetValue(item)?.ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    #endregion Selection identity

    // Paging Radzen last asked for. ClearIntent() (LoadDataAsync) swaps the
    // intent for a fresh one whose Take/Skip fall back to DataGridIntent's
    // defaults (10/0), so a direct ReloadDataAsync() - the sync button, or a
    // grid's search box - would otherwise silently snap the grid back to a
    // hardcoded page size and page 1, ignoring the user's saved grid settings.
    private int _lastTake = 10;
    private int _lastSkip;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
            if (Template is not null) DataGrid.Template = Template;
        }
    }

    public async Task LoadDataAsync(LoadDataArgs args)
    {

        if (ClientSide)
        {
            DGResult = DataGridResultVM<TItem>.New(Data, Data.Count);

            await InvokeAsync(StateHasChanged);
            return;
        }

        if (IsBusy)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        await InvokeAsync(StateHasChanged);

        foreach (var item in args.Filters?.Where(x => x.Type is null) ?? [])
        {
            var columnsCollection = DataGrid?.ColumnsCollection;
            var column = columnsCollection?.FirstOrDefault(x => x.Property == item.Property);
            if (column is not null)
            {
                item.Type = column.Type;
            }
        }

        if (DataGetter is not null)
        {
            DatagridAdapter = new DataGridIntentAdapter(args);

            DatagridAdapter.ClearFilters();
            DatagridAdapter.AdaptToMyFilterQueries();
            DatagridAdapter.AdaptToMySortQueries();
            DatagridAdapter.AdaptToPagination();

            DatagridAdapter.AddFilters([.. DefaultFilters]);

            DGResult = await DataGetter!(DatagridAdapter.QueryIntent);
        }

        // Row instances are re-materialized on every server-side load, so a
        // reference-identity check would drop the selection on the next load.
        // Match by business key instead and re-point the stored rows at the new
        // instances, mutating the list in place so the caller's list stays in sync.
        // Selections belonging to other pages are intentionally preserved.
        if (SelectedItems is { Count: > 0 })
        {
            var pageItems = (DataGrid?.Data ?? []).ToList();

            var freshByKey = new Dictionary<string, TItem>(pageItems.Count);
            var pageKeysAreUnique = true;

            foreach (var pageItem in pageItems)
            {
                var key = SelectionKey(pageItem);
                if (key is null)
                    continue;

                if (freshByKey.ContainsKey(key))
                {
                    pageKeysAreUnique = false;
                    break;
                }

                freshByKey[key] = pageItem;
            }

            // Only re-point when the key actually identifies a row uniquely;
            // otherwise keep the existing instances untouched.
            if (pageKeysAreUnique && freshByKey.Count > 0)
            {
                for (var i = 0; i < SelectedItems.Count; i++)
                {
                    var key = SelectionKey(SelectedItems[i]);
                    if (key is not null && freshByKey.TryGetValue(key, out var fresh))
                        SelectedItems[i] = fresh;
                }
            }
        }

        if (DatagridAdapter is not null)
        {
            // Capture before ClearIntent, which replaces QueryIntent and drops
            // the paging Radzen supplied in LoadDataArgs.
            _lastTake = DatagridAdapter.QueryIntent.Take;
            _lastSkip = DatagridAdapter.QueryIntent.Skip;

            DatagridAdapter.ClearIntent();
        }

        await InvokeAsync(StateHasChanged);
    }

    public async Task ReloadDataAsync()
    {
        if (DataGetter is not null && !ClientSide)
        {
            // Render the busy state before awaiting the fetch. LoadDataAsync
            // does this (line 114); without the same call here the only
            // StateHasChanged happens after the query returns, so IsLoading
            // flips true and false inside a single render and the grid's
            // loading spinner never reaches the client.
            await InvokeAsync(StateHasChanged);

            if (DatagridAdapter is not null)
            {
                DatagridAdapter.AddFilters([.. DefaultFilters]);

                DatagridAdapter.QueryIntent.Take = _lastTake;
                DatagridAdapter.QueryIntent.Skip = _lastSkip;
            }

            DGResult = await DataGetter(DatagridAdapter is null ? new() : DatagridAdapter.QueryIntent);

            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task ReloadDataAsync(IEnumerable<AppFilterDescriptor> filtersToRemove)
    {
        if (DataGetter is not null && !ClientSide)
        {
            if (filtersToRemove is not null)
                DatagridAdapter.RemoveFilters([.. filtersToRemove]);

            DGResult = await DataGetter(DatagridAdapter.QueryIntent);

            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid, settings => GridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.ReloadSettings();
        await DataGrid.Reload();
    }

}
