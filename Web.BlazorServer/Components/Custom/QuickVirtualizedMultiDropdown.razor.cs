using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;

namespace Web.BlazorServer.Components.Custom;

/// <summary>
///     Multi-select variant of QuickVirtualizedDropdown. Value is bound as
///     List&lt;TItem&gt; since RadzenDropDown's generic type param represents
///     the bound Value type, not the item type, and Multiple selection
///     requires a collection-typed Value.
/// </summary>
/// <typeparam name="TItem"></typeparam>
public partial class QuickVirtualizedMultiDropdown<TItem> : BaseComponent where TItem : class
{
    [Parameter] public List<TItem>? Value { get; set; } = null;
    [Parameter] public EventCallback<List<TItem>> ValueChanged { get; set; }
    [Parameter][EditorRequired] public required DataProvider DataGetter { get; set; }
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
    [Parameter] public RenderFragment? LoadingIndicator { get; set; } = null;
    [Parameter] public RenderFragment? EmptyTemplate { get; set; } = null;

    const int DEFAULT_TAKE_AMOUNT = 5;
    private int DataCount = 0;
    private RadzenDropDown<List<TItem>> Dropdown { get; set; } = default!;
    private List<TItem>? Data { get; set; } = null;
    private readonly Guid FallbackGuid = Guid.NewGuid();
    private string ActionString => ActionName is not null ? $"{ActionName}({IdString})" : IdString;
    private bool IsBusy => AppBusyService.IsBusy(ActionString);
    public string IdString => Id != null ? Id : Dropdown.UniqueID?.ToString() ?? FallbackGuid.ToString();
    private static object? GetPropertyValue(object item, string property) =>
    item.GetType().GetProperty(property)?.GetValue(item);
    async Task LoadDataAsync(LoadDataArgs args)
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

            var res = await DataGetter(DatagridAdapter.QueryIntent);

            AppBusyService.SetBusy(ActionString, true);

            return res;
        }, ActionString);

        action.OnSuccess((res) =>
        {
            var fetched = res.Data.ToList();
            var keyProperty = ValueProperty ?? TextProperty;

            if (Value is { Count: > 0 })
            {
                if (!string.IsNullOrEmpty(keyProperty))
                {
                    var selectedByKey = Value
                        .Where(v => v is not null)
                        .Select(v => (Key: GetPropertyValue(v!, keyProperty), Item: v))
                        .Where(x => x.Key is not null)
                        .GroupBy(x => x.Key!)
                        .ToDictionary(g => g.Key, g => g.First().Item);

                    for (int i = 0; i < fetched.Count; i++)
                    {
                        var key = GetPropertyValue(fetched[i]!, keyProperty);
                        if (key is not null && selectedByKey.TryGetValue(key, out var selectedInstance))
                            fetched[i] = selectedInstance!;
                    }
                }
                else
                {
                    // No usable key property at all - fall back to structural match
                    for (int i = 0; i < fetched.Count; i++)
                    {
                        var match = Value.FirstOrDefault(v => v is not null && StructurallyEqual(v!, fetched[i]!));
                        if (match is not null)
                            fetched[i] = match;
                    }
                }
            }

            Data = fetched;
            DataCount = res.Count;
            return Task.CompletedTask;
        });
    }

    private static bool StructurallyEqual(object a, object b)
    {
        if (a.GetType() != b.GetType()) return false;

        foreach (var prop in a.GetType().GetProperties())
        {
            if (!prop.CanRead) continue;
            var av = prop.GetValue(a);
            var bv = prop.GetValue(b);
            if (!Equals(av, bv)) return false;
        }

        return true;
    }

    public void Reset() => Dropdown.Reset();

    public delegate Task<(IEnumerable<TItem> Data, int Count)> DataProvider(DataGridIntent intent);
}