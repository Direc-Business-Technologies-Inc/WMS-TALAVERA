using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Custom;

public partial class QuickVirtualizedDropdown<TItem> : BaseComponent where TItem : class
{
    [Parameter] public TItem? Value { get; set; } = null;
    [Parameter] public EventCallback<TItem> ValueChanged { get; set; }
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
    private RadzenDropDown<TItem> Dropdown { get; set; } = default!;
    private List<TItem>? Data { get; set; }  = null;
    private readonly Guid FallbackGuid = Guid.NewGuid();
    private string ActionString => ActionName is not null ? $"{ActionName}({IdString})" : IdString;
    private bool IsBusy => AppBusyService.IsBusy(ActionString);
    public string IdString => Id != null ? Id : Dropdown.UniqueID?.ToString() ?? FallbackGuid.ToString();

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
            Data = [.. res.Data];
            DataCount = res.Count;
            return Task.CompletedTask;
        });
    }

    public void Reset() => Dropdown.Reset();

    public delegate Task<(IEnumerable<TItem> Data, int Count)> DataProvider(DataGridIntent intent);
}
