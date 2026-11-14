using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using System.Reflection;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppDataGrid<TItem> : BaseComponent, IDisposable where TItem : class
{
    #region Injects
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IHttpContextAccessor ContextAccessor { get; set; } = default!;
    #endregion Injects

    #region Parameter

    [Parameter] public string Id { get; init; } = $"{typeof(TItem).Name}-data-grid";
    [Parameter] public List<TItem> Data { get; set; } = new List<TItem>();
    [Parameter] public EventCallback<List<TItem>> DataChanged { get; set; }
    [Parameter] public Func<DataGridIntent, Task<DataGridResultVM<TItem>>>? DataGetter { get; set; }
    [Parameter] public Action<DataGridIntent>? PrepareIntent { get; set; }
    [Parameter] public EventCallback OnDataLoaded { get; set; }
    [Parameter] public RenderFragment HeaderTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    [Parameter] public RenderFragment Footer { get; set; }
    [Parameter] public RenderFragment LoadingTemplate { get; set; }
    [Parameter] public RenderFragment DetailTemplate { get; set; }
    [Parameter] public RenderFragment EmptyTemplate { get; set; }
    [Parameter] public RenderFragment GridColumns { get; set; }
    [Parameter] public bool ServerSide { get; set; } = true;
    [Parameter] public DataGridSettings GridSettings { get; set; } = new();
    [Parameter] public EventCallback<DataGridSettings> GridSettingsChanged { get; set; }
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; set; }
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowClick { get; set; }
    [Parameter] public EventCallback<TItem> OnRowSelect { get; set; }
    [Parameter] public EventCallback<TItem> OnRowDeselect { get; set; }
    [Parameter] public DataGridSelectionMode DataGridSelectionMode { get; set; } = DataGridSelectionMode.Single;
    [Parameter] public bool GridSettingsLoaded { get; set; } = false;
    // Some existing grids load persisted settings in their owning component.
    // Let them opt out of the wrapper's own first-render settings load.
    [Parameter] public bool AutoLoadGridSettings { get; set; } = true;
    // Owning pages load persisted settings asynchronously. Suppress Radzen's
    // visibility/settings callbacks until one post-render settings application
    // can start the initial read with fully initialized columns and paging.
    [Parameter] public bool DeferInitialLoad { get; set; }
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
    protected bool IsBusy => _isLoading || AppBusyService.IsBusy(ActionName);
    public GridRefreshOutcome LastRefreshOutcome { get; private set; } = GridRefreshOutcome.Succeeded;
    public string? RefreshError { get; private set; }
    public Exception? RefreshException { get; private set; }
    private string? _successfulScope;
    private string? _successfulCountScope;
    private DataGridResultVM<TItem>? _lastSuccessfulResult;
    private long _generation;
    private bool _disposed;
    private LoadRequest? _pending;
    private LoadRequest? _activeRequest;
    private TaskCompletionSource<GridRefreshOutcome>? _navigationCompletion;
    private LoadRequest? _navigationRequest;
    private CancellationTokenSource? _activeCancellation;
    private DataGridIntent _lastIntent = new();
    private sealed record LoadRequest(DataGridIntent Intent, string Scope, string CountScope, string Identity, long Generation,
        Func<DataGridIntent, Task<DataGridResultVM<TItem>>> Getter, TaskCompletionSource<GridRefreshOutcome> Completion);

    private string CurrentIdentity() => $"{CurrentUserService?.UserId}:{CurrentUserService?.NsSubsidiaryId}";

    private string AuthorizationScope()
    {
        var claims = (ContextAccessor?.HttpContext?.User?.Claims.Select(c => $"{c.Type}={c.Value}")
            ?? Enumerable.Empty<string>()).OrderBy(c => c, StringComparer.Ordinal);
        var authorization = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join("|", claims))));
        return authorization;
    }

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

    // Retain Radzen's complete last intent, including saved paging, for Sync.
    private bool _isLoading;
    private bool _initialLoadStarted;

    private async Task OnGridSettingsChanged(DataGridSettings settings)
    {
        if (DeferInitialLoad && !_initialLoadStarted) return;
        GridSettings = await GridSettingsService.GridSettingsChanged(DataGrid!, settings);
        await GridSettingsChanged.InvokeAsync(GridSettings);
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (AutoLoadGridSettings) await LoadGridSettings();
            if (Template is not null) DataGrid.Template = Template;
        }
        if (DeferInitialLoad && GridSettingsLoaded && DataGetter is not null && !_initialLoadStarted && !_disposed)
        {
            _initialLoadStarted = true;
            await DataGrid!.ReloadSettings(forceReload: true);
        }
    }

    public async Task LoadDataAsync(LoadDataArgs args)
    {
        if (_disposed) return;
        if (DeferInitialLoad && !_initialLoadStarted) return;
        var navigation = _navigationCompletion;

        if (ClientSide)
        {
            DGResult = DataGridResultVM<TItem>.New(Data, Data.Count);
            navigation?.TrySetResult(GridRefreshOutcome.Succeeded);

            await InvokeAsync(StateHasChanged);
            return;
        }

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

            _lastIntent = DatagridAdapter.QueryIntent.Snapshot();
            var intent = _lastIntent.Snapshot();
            if (navigation is not null) intent.ReadPolicy = GridReadPolicy.Fresh;
            var outcome = await RequestLoadAsync(intent, navigation is not null);
            navigation?.TrySetResult(outcome);
        }
        else navigation?.TrySetResult(GridRefreshOutcome.Cancelled);
    }

    private Task<GridRefreshOutcome> RequestLoadAsync(DataGridIntent intent, bool navigationCallback = false,
        IEnumerable<AppFilterDescriptor>? filtersToRemove = null)
    {
        if (_disposed) return Task.FromResult(GridRefreshOutcome.Cancelled);
        var snapshot = intent.Snapshot();
        snapshot.Filters.AddRange(new DataGridIntent { Filters = [.. DefaultFilters] }.Snapshot().Filters);
        if (filtersToRemove is not null)
            snapshot.Filters.RemoveAll(f => filtersToRemove.Any(remove => remove.Property == f.Property && Equals(remove.Value, f.Value)));
        PrepareIntent?.Invoke(snapshot);
        var identity = CurrentIdentity();
        var scopePrefix = identity + ":" + AuthorizationScope() + ":";
        var scope = scopePrefix + System.Text.Json.JsonSerializer.Serialize(snapshot);
        // Radzen may emit the same first-page callback again while GoToPage is
        // reconciling. Share that navigation's result, including failure, only
        // while it remains the latest request. Direct refreshes always enqueue.
        if (navigationCallback && _navigationRequest is { } previous &&
            previous.Generation == _generation && previous.Scope == scope)
            return previous.Completion.Task;
        var countIntent = snapshot.Snapshot();
        countIntent.Skip = 0;
        countIntent.Take = 0;
        var countScope = scopePrefix + System.Text.Json.JsonSerializer.Serialize(countIntent);
        var request = new LoadRequest(snapshot, scope, countScope, identity, ++_generation, DataGetter!,
            new(TaskCreationOptions.RunContinuationsAsynchronously));
        if (navigationCallback) _navigationRequest = request;
        _pending?.Completion.TrySetResult(GridRefreshOutcome.Superseded);
        _activeRequest?.Completion.TrySetResult(GridRefreshOutcome.Superseded);
        _pending = request;
        // A page change hides old-page rows but keeps the same-filter total so
        // Radzen does not reset its pager to page one while the request is loading.
        DGResult = _successfulScope == scope && _lastSuccessfulResult is not null ? _lastSuccessfulResult
            : new() { Count = _successfulCountScope == countScope ? _lastSuccessfulResult?.Count ?? 0 : 0 };
        RefreshError = null;
        RefreshException = null;
        _activeCancellation?.Cancel();
        if (!_isLoading) _ = ProcessLoadsAsync();
        return request.Completion.Task;
    }

    private async Task ProcessLoadsAsync()
    {
        _isLoading = true;
        try
        {
            while (_pending is { } request && !_disposed)
            {
                _pending = null;
                _activeRequest = request;
                using var cancellation = new CancellationTokenSource();
                _activeCancellation = cancellation;
                request.Intent.CancellationToken = cancellation.Token;
                try
                {
                    await InvokeAsync(StateHasChanged);
                    if (request.Identity != CurrentIdentity())
                        throw new OperationCanceledException("Grid scope changed before the read started.");
                    var result = await request.Getter(request.Intent);
                    if (_disposed || request.Generation != _generation || request.Identity != CurrentIdentity())
                    {
                        if (!_disposed && request.Generation == _generation && request.Identity != CurrentIdentity())
                        {
                            DGResult = new();
                            LastRefreshOutcome = GridRefreshOutcome.Superseded;
                            RefreshError = "User or subsidiary changed. Retry Sync.";
                        }
                        request.Completion.TrySetResult(GridRefreshOutcome.Superseded);
                        continue;
                    }
                    DGResult = result;
                    _lastSuccessfulResult = result;
                    _successfulScope = request.Scope;
                    _successfulCountScope = request.CountScope;
                    RefreshError = null;
                    RefreshException = null;
                    LastRefreshOutcome = GridRefreshOutcome.Succeeded;
                    ReconcileSelection();
                    await OnDataLoaded.InvokeAsync();
                    request.Completion.TrySetResult(LastRefreshOutcome);
                }
                catch (Exception ex)
                {
                    var outcome = _disposed ? GridRefreshOutcome.Cancelled
                        : request.Generation != _generation || request.Identity != CurrentIdentity() ? GridRefreshOutcome.Superseded
                        : ex is OperationCanceledException ? GridRefreshOutcome.Cancelled
                        : ex is GridAdmissionBusyException ? GridRefreshOutcome.Busy : GridRefreshOutcome.Failed;
                    if (!_disposed && request.Generation == _generation)
                    {
                        LastRefreshOutcome = outcome;
                        RefreshException = ex;
                        if (request.Identity != CurrentIdentity())
                        {
                            DGResult = new();
                            RefreshError = "User or subsidiary changed. Retry Sync.";
                        }
                        if (outcome is GridRefreshOutcome.Failed or GridRefreshOutcome.Busy)
                        {
                            RefreshError = _successfulScope == request.Scope
                                ? "Refresh failed. Displayed rows are stale. Retry Sync."
                                : "Could not load the selected scope. Retry Sync.";
                            if (_successfulScope != request.Scope) DGResult = new();
                            if (outcome == GridRefreshOutcome.Busy) RefreshError = "NetSuite is busy. " + RefreshError;
                        }
                        else if (outcome == GridRefreshOutcome.Cancelled)
                            RefreshError = "Load cancelled. Retry Sync; retained rows have not been refreshed.";
                    }
                    request.Completion.TrySetResult(outcome);
                }
                finally { _activeCancellation = null; _activeRequest = null; }
            }
        }
        finally
        {
            _isLoading = false;
            if (!_disposed) await InvokeAsync(StateHasChanged);
        }
    }

    private void ReconcileSelection()
    {

        // Row instances are re-materialized on every server-side load, so a
        // reference-identity check would drop the selection on the next load.
        // Match by business key instead and re-point the stored rows at the new
        // instances, mutating the list in place so the caller's list stays in sync.
        // Selections belonging to other pages are intentionally preserved.
        if (SelectedItems is { Count: > 0 })
        {
            var pageItems = DGResult.Items.ToList();

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

    }

    public Task<GridRefreshOutcome> ReloadDataAsync()
    {
        var intent = _lastIntent.Snapshot();
        intent.ReadPolicy = GridReadPolicy.Fresh;
        return DataGetter is null || ClientSide ? Task.FromResult(GridRefreshOutcome.Succeeded) : RequestLoadAsync(intent);
    }

    public Task<GridRefreshOutcome> ReloadDataAsync(IEnumerable<AppFilterDescriptor> filtersToRemove)
    {
        filtersToRemove ??= [];
        _lastIntent.Filters.RemoveAll(f => filtersToRemove.Any(remove => remove.Property == f.Property && Equals(remove.Value, f.Value)));
        var intent = _lastIntent.Snapshot();
        intent.ReadPolicy = GridReadPolicy.Fresh;
        return DataGetter is null || ClientSide ? Task.FromResult(GridRefreshOutcome.Succeeded)
            : RequestLoadAsync(intent, filtersToRemove: filtersToRemove);
    }

    public async Task<GridRefreshOutcome> ReloadFirstPageAsync()
    {
        if (_disposed) return GridRefreshOutcome.Cancelled;
        if (DataGrid is null || DataGrid.CurrentPage == 0) return await ReloadDataAsync();
        var completion = new TaskCompletionSource<GridRefreshOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        _navigationCompletion?.TrySetResult(GridRefreshOutcome.Superseded);
        _navigationCompletion = completion;
        _navigationRequest = null;
        try
        {
            await DataGrid.GoToPage(0);
            // Some Radzen configurations have no server callback (e.g. client-side grids).
            if (ReferenceEquals(_navigationCompletion, completion) && _navigationRequest is null)
                completion.TrySetResult(GridRefreshOutcome.Cancelled);
            return await completion.Task;
        }
        finally
        {
            if (ReferenceEquals(_navigationCompletion, completion))
            {
                _navigationCompletion = null;
                _navigationRequest = null;
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        ++_generation;
        _activeCancellation?.Cancel();
        _activeRequest?.Completion.TrySetResult(GridRefreshOutcome.Cancelled);
        _pending?.Completion.TrySetResult(GridRefreshOutcome.Cancelled);
        _navigationCompletion?.TrySetResult(GridRefreshOutcome.Cancelled);
        _pending = null;
    }

    public async Task RefreshOrThrowAsync()
    {
        var outcome = await ReloadDataAsync();
        EnsureRefreshSucceeded(outcome);
    }

    public async Task RefreshFirstPageOrThrowAsync()
    {
        var outcome = await ReloadFirstPageAsync();
        EnsureRefreshSucceeded(outcome);
    }

    private static void EnsureRefreshSucceeded(GridRefreshOutcome outcome)
    {
        if (outcome != GridRefreshOutcome.Succeeded)
            throw new InvalidOperationException($"Refresh needed ({outcome}). Retry Sync.");
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid, settings => GridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.ReloadSettings();
        await DataGrid.Reload();
    }

}
