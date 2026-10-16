using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Custom;

/// <summary>
///     Basically just a wrapper for a radzen dropdown just made so
///     I dont have to bother rewriting code for the loaddataasync.
///     Actions are all run as loading actions so that when there
///     is an error, it is shown to the user -- this may not be needed
///     and is probably best to hide the error notif when not in dev 
///     environment
///     Has renderfragments for loading indicators and empty template
/// </summary>
/// <typeparam name="TItem"></typeparam>
public partial class QuickVirtualizedDropdown<TItem> : BaseComponent, IAsyncDisposable where TItem : class
{
    [Parameter] public TItem? Value { get; set; } = null;
    [Parameter] public EventCallback<TItem> ValueChanged { get; set; }
    [Parameter][EditorRequired] public required DataProvider DataGetter { get; set; }
    /// <summary>
    /// Optional cancellation-aware provider. When supplied it is preferred
    /// over <see cref="DataGetter"/> so a superseded load or disposal aborts
    /// the in-flight NetSuite request (Req 3.1). Existing single-argument
    /// providers keep working unchanged.
    /// </summary>
    [Parameter] public Func<DataGridIntent, CancellationToken, Task<(IEnumerable<TItem> Data, int Count)>>? CancellableDataGetter { get; set; }
    [Parameter] public string? ActionName { get; set; } = null;
    [Parameter] public string? FilterTarget { get; set; } = null;
    [Parameter] public string? TextProperty { get; set; } = null;
    [Parameter] public string? ValueProperty { get; set; } = null;
    [Parameter] public string? Name { get; set; } = null;
    [Parameter] public string? Id { get; set; } = null;
    [Parameter] public ComparisonOperatorEnum FilterOperator { get; set; } = ComparisonOperatorEnum.Contains;
    [Parameter] public bool AllowClear { get; set; } = true;
    [Parameter] public bool AllowFiltering { get; set; } = true;
    [Parameter] public bool ShowLoadingIndicator { get; set; } = true;
    [Parameter] public bool ShowEmptyTemplate { get; set; } = true;
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public bool Disabled { get; set; } = false;
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool Multiple { get; set; } = false;
    [Parameter] public RenderFragment? LoadingIndicator { get; set; } = null;
    [Parameter] public RenderFragment? EmptyTemplate { get; set; } = null;

    const int DEFAULT_TAKE_AMOUNT = 5;
    private int DataCount = 0;
    private RadzenDropDown<TItem> Dropdown { get; set; } = default!;
    private List<TItem>? Data { get; set; }  = null;
    private readonly Guid FallbackGuid = Guid.NewGuid();
    private CancellationTokenSource? _loadCts;
    private string ActionString => ActionName is not null ? $"{ActionName}({IdString})" : IdString;
    private bool IsBusy => AppBusyService.IsBusy(ActionString);
    public string IdString => Id != null ? Id : Dropdown.UniqueID?.ToString() ?? FallbackGuid.ToString();

    async Task LoadDataAsync(LoadDataArgs args)
    {
        // A new filter load supersedes any in-flight one; disposal
        // (navigation away) cancels the current load (Req 3.1, 4.3).
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;
        var cancellableGetter = CancellableDataGetter;

        try
        {
            var action = await AppActionFactory.RunLoadingAsync(async () =>
            {
                AppBusyService.SetBusy(ActionString, true);

                var DatagridAdapter = new DataGridIntentAdapter(args);
                DatagridAdapter.AdaptToPagination();
                if (DatagridAdapter.QueryIntent.Take <= 0)
                    DatagridAdapter.QueryIntent.Take = DEFAULT_TAKE_AMOUNT;

                if (!(string.IsNullOrEmpty(args.Filter) || string.IsNullOrEmpty(FilterTarget)))
                {
                    DatagridAdapter.QueryIntent.Filters.Add(new()
                    {
                        LogicalOperator = LogicalOperatorEnum.AND,
                        Property = FilterTarget,
                        Value = args.Filter,
                        ComparisonOperator = FilterOperator
                    });
                }


                var res = cancellableGetter is not null
                    ? await cancellableGetter(DatagridAdapter.QueryIntent, ct)
                    : await DataGetter(DatagridAdapter.QueryIntent);

                ct.ThrowIfCancellationRequested();

                AppBusyService.SetBusy(ActionString, true);

                return res;
            }, ActionString);

            // Stale or cancelled loads never touch the UI (Req 3.3).
            if (action.IsCancelled || ct.IsCancellationRequested)
                return;

            action.OnSuccess((res) =>
            {
                if (ct.IsCancellationRequested)
                    return Task.CompletedTask;
                Data = [.. res.Data];
                DataCount = res.Count;
                return Task.CompletedTask;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation-away / superseded load: stop silently (Req 3.3, 5.2).
        }
    }

    public void Reset() => Dropdown.Reset();

    public async ValueTask DisposeAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }

    public delegate Task<(IEnumerable<TItem> Data, int Count)> DataProvider(DataGridIntent intent);
}
