using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using System.Linq.Expressions;
using System.Reflection;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Enums;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppDataGridPicker<TItem> : BaseComponent where TItem : class
{

    #region Injects
    [Inject] IGridSettingsService GridSettingsService { get; set; }
    #endregion Injects

    #region Parameter
    [Parameter] public PageActionTypeEnum PageState { get; set; } = PageActionTypeEnum.View;
    [Parameter] public EventCallback<PageActionTypeEnum> PageStateChanged { get; set; }
    [Parameter] public RenderFragment Columns { get; set; }
    [Parameter] public Func<DataGridIntent, Task<DataGridResultVM<TItem>>>? LoadDataAsync { get; set; }
    [Parameter] public DataGridSettings SourceDataGridSettings { get; set; } = new();
    [Parameter] public DataGridSettings TargetDataGridSettings { get; set; } = new();
    [Parameter] public string SourceActionName { get; set; } = string.Empty;
    [Parameter] public string SourceGridId { get; set; } = string.Empty;
    [Parameter] public string TargetGridId { get; set; } = string.Empty;
    [Parameter] public string FilterProperty { get; set; } = string.Empty;
    [Parameter] public bool Disabled { get; set; } = false;
    [Parameter] public EventCallback<bool> DisabledChanged { get; set; }
    [Parameter] public List<TItem> TargetItems { get; set; } = [];
    [Parameter] public EventCallback<List<TItem>> TargetItemsChanged { get; set; }
    [Parameter] public Func<Task>? OverrideTransferSelected { get; set; } = null;
    [Parameter] public Func<Task>? OverrideMassTransfer { get; set; } = null;
    [Parameter] public Func<Task>? OverrideRemoveSelected { get; set; } = null;
    [Parameter] public Func<Task>? OverrideMassRemove { get; set; } = null;
    [Parameter] public Func<List<TItem>, Task<bool>>? TransferSelectedValidation { get; set; } = null;
    [Parameter] public Func<List<TItem>, Task<bool>>? MassTransferValidation { get; set; } = null;
    [Parameter] public Func<List<TItem>, Task<bool>>? RemoveSelectedValidation { get; set; } = null;
    [Parameter] public Func<List<TItem>, Task<bool>>? MassRemoveValidation { get; set; } = null;
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; set; }
    #endregion Parameter

    #region Primitives
    public bool SourceGridSettingsLoaded { get; set; } = false;
    public bool TargetGridSettingsLoaded { get; set; } = false;
    #endregion Primitives

    #region Custom Class
    public EventCallback<bool> SourceGridSettingsLoadedChanged { get; set; }
    public AppDataGrid<TItem> SourceDataGrid { get; set; } = default!;
    public EventCallback<bool> TargetGridSettingsLoadedChanged { get; set; }
    public AppDataGrid<TItem> TargetDataGrid { get; set; } = default!;
    #endregion Custom Class

    #region Data Structures
    public IList<TItem> SelectedSourceItems { get; set; } = [];
    public IList<TItem> SelectedTargetItems { get; set; } = [];
    public List<AppFilterDescriptor> SelectedSourceItemsFilter { get; set; } = [];
    #endregion Data Structures

    #region Overrides
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadSourceGridSettings();
            await LoadTargetGridSettings();

            await InvokeAsync(StateHasChanged);
        }
    }
    #endregion Overrides

    #region Custom Function
    async Task LoadSourceGridSettings()
    {
        await GridSettingsService.SetGridSettings(SourceDataGrid.DataGrid, settings => SourceDataGridSettings = settings ?? new());
        SourceGridSettingsLoaded = true;

        await SourceDataGrid.DataGrid.ReloadSettings();
        await SourceDataGrid.DataGrid.Reload();
    }

    async Task LoadTargetGridSettings()
    {
        await GridSettingsService.SetGridSettings(TargetDataGrid.DataGrid, settings => TargetDataGridSettings = settings ?? new());
        TargetGridSettingsLoaded = true;

        await TargetDataGrid.DataGrid.ReloadSettings();
        await TargetDataGrid.DataGrid.Reload();
    }

    async Task OnSourceRowSelectAsync(TItem item)
    {

        if (FilterProperty is not null)
        {
            PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);

            if (propertyInfo is not null)
            {
                object? valueToMatch = propertyInfo.GetValue(item);
                if (valueToMatch is not null)
                {
                    var param = Expression.Parameter(typeof(TItem), "x");
                    var propertyAccess = Expression.Property(param, propertyInfo);

                    var constant = Expression.Constant(valueToMatch, propertyInfo.PropertyType);

                    var equality = Expression.Equal(propertyAccess, constant);
                    var lambda = Expression.Lambda<Func<TItem, bool>>(equality, param);

                    var filteredItems = SourceDataGrid.DGResult.Items.AsQueryable()
                        .Where(lambda)
                        .ToList();

                    foreach (var fi in filteredItems)
                        SelectedSourceItems.Add(fi);
                }
            }
        }

    }

    async Task OnSourceRowDeselectAsync(TItem item)
    {
        if (!SelectedSourceItems.Contains(item))
            SelectedSourceItems.Remove(item);

        if (FilterProperty is not null)
        {
            PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);

            if (propertyInfo is not null)
            {
                object? valueToMatch = propertyInfo.GetValue(item);
                if (valueToMatch is not null)
                {
                    var param = Expression.Parameter(typeof(TItem), "x");
                    var propertyAccess = Expression.Property(param, propertyInfo);

                    var constant = Expression.Constant(valueToMatch, propertyInfo.PropertyType);

                    var equality = Expression.Equal(propertyAccess, constant);
                    var lambda = Expression.Lambda<Func<TItem, bool>>(equality, param);

                    var filteredItems = SelectedSourceItems.AsQueryable()
                        .Where(lambda)
                        .ToList();

                    foreach (var fi in filteredItems)
                        SelectedSourceItems.Remove(fi);
                }
            }
        }
    }

    async Task OnTargetRowSelectAsync(TItem item)
    {

        if (FilterProperty is not null)
        {
            PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);

            if (propertyInfo is not null)
            {
                object? valueToMatch = propertyInfo.GetValue(item);
                if (valueToMatch is not null)
                {
                    var param = Expression.Parameter(typeof(TItem), "x");
                    var propertyAccess = Expression.Property(param, propertyInfo);

                    var constant = Expression.Constant(valueToMatch, propertyInfo.PropertyType);

                    var equality = Expression.Equal(propertyAccess, constant);
                    var lambda = Expression.Lambda<Func<TItem, bool>>(equality, param);

                    var filteredItems = TargetItems.AsQueryable()
                        .Where(lambda)
                        .ToList();

                    foreach (var fi in filteredItems)
                        SelectedTargetItems.Add(fi);
                }
            }
        }

    }

    async Task OnTargetRowDeselectAsync(TItem item)
    {
        if (!SelectedTargetItems.Contains(item))
            SelectedTargetItems.Remove(item);

        if (FilterProperty is not null)
        {
            PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);

            if (propertyInfo is not null)
            {
                object? valueToMatch = propertyInfo.GetValue(item);
                if (valueToMatch is not null)
                {
                    var param = Expression.Parameter(typeof(TItem), "x");
                    var propertyAccess = Expression.Property(param, propertyInfo);

                    var constant = Expression.Constant(valueToMatch, propertyInfo.PropertyType);

                    var equality = Expression.Equal(propertyAccess, constant);
                    var lambda = Expression.Lambda<Func<TItem, bool>>(equality, param);

                    var filteredItems = SelectedTargetItems.AsQueryable()
                        .Where(lambda)
                        .ToList();

                    foreach (var fi in filteredItems)
                        SelectedTargetItems.Remove(fi);

                    SelectedSourceItemsFilter.RemoveAll(sif => sif.Property == propertyInfo.Name);
                }
            }
        }
    }

    async Task TransferSelected()
    {
        if (SelectedSourceItems.Count <= 0)
            return;

        if (TransferSelectedValidation is not null)
            if (!await TransferSelectedValidation([.. SelectedSourceItems]))
                return;

        if (OverrideTransferSelected is not null)
        {
            await OverrideTransferSelected();
            return;
        }

        // Add filters for transferred items to exclude them from source
        PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);
        if (propertyInfo is not null)
        {
            foreach (var item in SelectedSourceItems)
            {
                var filterValue = propertyInfo.GetValue(item);

                // Only add if this filter doesn't already exist
                if (!SelectedSourceItemsFilter.Any(f =>
                    f.Property == propertyInfo.Name &&
                    Equals(f.Value, filterValue)))
                {
                    SelectedSourceItemsFilter.Add(new AppFilterDescriptor()
                    {
                        LogicalOperator = LogicalOperatorEnum.AND,
                        Property = propertyInfo.Name,
                        Value = filterValue,
                        FilterValueType = FilterValueTypeEnum.String,
                        ComparisonOperator = ComparisonOperatorEnum.NotEquals,
                    });
                }
            }
        }

        TargetItems.AddRange(SelectedSourceItems);
        await TargetItemsChanged.InvokeAsync(TargetItems);
        SelectedSourceItems.Clear();
        SelectedTargetItems.Clear();

        await SourceDataGrid.ReloadDataAsync();
        await TargetDataGrid.DataGrid.Reload();
    }

    async Task RemoveSelected()
    {
        if (SelectedTargetItems.Count <= 0)
            return;

        if (RemoveSelectedValidation is not null)
            if (!await RemoveSelectedValidation([.. SelectedSourceItems]))
                return;

        if (OverrideRemoveSelected is not null)
        {
            await OverrideRemoveSelected();
            return;
        }

        PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);
        if (propertyInfo is not null)
        {
            // Remove filters for items being moved back to source
            foreach (var item in SelectedTargetItems)
            {
                var filterValue = propertyInfo.GetValue(item);

                // Remove the NotEquals filter for this item
                SelectedSourceItemsFilter.RemoveAll(f =>
                    f.Property == propertyInfo.Name &&
                    Equals(f.Value, filterValue) &&
                    f.ComparisonOperator == ComparisonOperatorEnum.NotEquals);

                // Remove from target using FilterProperty grouping
                if (filterValue is not null)
                {
                    var param = Expression.Parameter(typeof(TItem), "x");
                    var propertyAccess = Expression.Property(param, propertyInfo);
                    var constant = Expression.Constant(filterValue, propertyInfo.PropertyType);
                    var equality = Expression.Equal(propertyAccess, constant);
                    var lambda = Expression.Lambda<Func<TItem, bool>>(equality, param);

                    var filteredItems = TargetItems.AsQueryable()
                        .Where(lambda)
                        .ToList();

                    foreach (var fi in filteredItems)
                        TargetItems.Remove(fi);
                }
            }
        }


        await TargetItemsChanged.InvokeAsync(TargetItems);
        SelectedSourceItems.Clear();
        SelectedTargetItems.Clear();

        await SourceDataGrid.ReloadDataAsync();
        await TargetDataGrid.DataGrid.Reload();

    }

    async Task MassTransfer()
    {
        if (MassTransferValidation is not null)
            if (!await MassTransferValidation([.. SourceDataGrid.DGResult.Items]))
                return;

        if (OverrideMassTransfer is not null)
        {
            await OverrideMassTransfer();
            return;
        }

        // Add filters for all transferred items to exclude them from source
        PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);
        if (propertyInfo is not null)
        {
            foreach (var item in SourceDataGrid.DGResult.Items)
            {
                var filterValue = propertyInfo.GetValue(item);

                // Only add if this filter doesn't already exist
                if (!SelectedSourceItemsFilter.Any(f =>
                    f.Property == propertyInfo.Name &&
                    Equals(f.Value, filterValue)))
                {
                    SelectedSourceItemsFilter.Add(new AppFilterDescriptor()
                    {
                        LogicalOperator = LogicalOperatorEnum.AND,
                        Property = propertyInfo.Name,
                        Value = filterValue,
                        FilterValueType = FilterValueTypeEnum.String,
                        ComparisonOperator = ComparisonOperatorEnum.NotEquals,
                    });
                }
            }
        }

        TargetItems.AddRange(SourceDataGrid.DGResult.Items);
        await TargetItemsChanged.InvokeAsync(TargetItems);
        SelectedSourceItems.Clear();
        SelectedTargetItems.Clear();


        await SourceDataGrid.ReloadDataAsync();
        await TargetDataGrid.DataGrid.Reload();
    }

    async Task MassRemove()
    {
        if (MassRemoveValidation is not null)
            if (!await MassRemoveValidation([.. TargetItems]))
                return;

        if (OverrideMassRemove is not null)
        {
            await OverrideMassRemove();
            return;
        }

        PropertyInfo? propertyInfo = typeof(TItem).GetProperty(FilterProperty);
        if (propertyInfo is not null)
        {
            // Remove all filters for items being moved back to source
            foreach (var item in TargetItems)
            {
                var filterValue = propertyInfo.GetValue(item);

                // Remove the NotEquals filter for this item
                SelectedSourceItemsFilter.RemoveAll(f =>
                    f.Property == propertyInfo.Name &&
                    Equals(f.Value, filterValue) &&
                    f.ComparisonOperator == ComparisonOperatorEnum.NotEquals);
            }
        }

        await TargetItemsChanged.InvokeAsync(TargetItems);
        SelectedSourceItems.Clear();
        SelectedTargetItems.Clear();
        SelectedSourceItemsFilter.Clear();
        TargetItems.Clear();

        await SourceDataGrid.ReloadDataAsync();
        await TargetDataGrid.DataGrid.Reload();
    }

    public void ClearDefaultFilters() => SelectedSourceItemsFilter.Clear();


    #endregion Custom Function
}
