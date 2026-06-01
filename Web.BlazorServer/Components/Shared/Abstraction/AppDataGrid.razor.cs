using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
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

    bool _isFirstLoad { get; set; } = true;
    public RadzenDataGrid<TItem> DataGrid { get; set; } = default!;
    public DataGridResultVM<TItem> DGResult { get; set; } = new DataGridResultVM<TItem>();
    public EventCallback<DataGridResultVM<TItem>> DGResultChanged { get; set; }
    public IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    protected bool IsBusy => AppBusyService.IsBusy(ActionName);


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
        }
    }

    public async Task LoadDataAsync(LoadDataArgs args)
    {

        if (IsBusy || ClientSide)
        {
            DGResult = DataGridResultVM<TItem>.New(Data, Data.Count);

            await InvokeAsync(StateHasChanged);
            return;
        }

        await InvokeAsync(StateHasChanged);

        foreach (var item in args.Filters.Where(x => x.Type is null))
        {
            var columnsCollection = DataGrid.ColumnsCollection;
            var column = columnsCollection.FirstOrDefault(x => x.Property == item.Property);
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

        SelectedItems = [.. DataGrid.Data.Where(d => SelectedItems.Contains(d))];
        if (DatagridAdapter is not null)
            DatagridAdapter.ClearIntent();

        _isFirstLoad = false;
        await InvokeAsync(StateHasChanged);
    }

    public async Task ReloadDataAsync()
    {
        if (DataGetter is not null && !ClientSide)
        {
            DatagridAdapter?.AddFilters([.. DefaultFilters]);

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
