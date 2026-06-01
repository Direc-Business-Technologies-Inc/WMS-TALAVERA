using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Text.Json;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class GridSettingsService(
    IJSRuntime JSRuntime)
    : IGridSettingsService
{
    public async Task<DataGridSettings> GridSettingsChanged<TItem>(RadzenDataGrid<TItem> grid, DataGridSettings newSettings)
    {
        await SaveStateAsync(grid, newSettings);
        return newSettings;
    }

    public async Task<DataGridSettings?> LoadStateAsync<T>(RadzenDataGrid<T> grid)
    {
        await Task.CompletedTask;
        DataGridSettings? gridSettings = null;
        string? savedGridSettings = null, gridOtherSettings = null;

        if (grid is not null && grid.Attributes.TryGetValue("id", out var value))
        {
            await LoadSettingAsync($"{value}-TSET", settings => savedGridSettings = settings);
            await LoadSettingAsync($"{value}-OTSET", settings => gridOtherSettings = settings);

            if (!string.IsNullOrEmpty(savedGridSettings))
            {
                gridSettings = JsonSerializer.Deserialize<DataGridSettings>(savedGridSettings);
            }

            if (!string.IsNullOrEmpty(gridOtherSettings))
            {
                OtherTableSettings? ots = JsonSerializer.Deserialize<OtherTableSettings>(gridOtherSettings);
                if (ots is not null)
                {
                    grid.FilterMode = ots.FilterMode;
                    grid.AllowPaging = ots.AllowPaging;
                    grid.AllowColumnPicking = ots.AllowColumnPicking;
                    grid.AllowColumnResize = ots.AllowColumnResize;
                    grid.AllowFiltering = ots.AllowFiltering;
                    grid.AllowSorting = ots.AllowSorting;
                    grid.GridLines = ots.GridLines;
                    grid.UpdatePickableColumns();
                    PagingChanged(grid, ots.AllowPaging);
                }
            }
        }

        return gridSettings;
    }
    public async Task SaveStateAsync<TItem>(RadzenDataGrid<TItem> grid, DataGridSettings gridSettings)
    {
        await Task.CompletedTask;

        if (grid is not null && grid.Attributes.TryGetValue("id", out var value))
        {
            OtherTableSettings ots = new(
                grid.FilterMode,
                grid.AllowPaging,
                grid.AllowColumnPicking,
                grid.AllowColumnResize,
                grid.AllowFiltering,
                grid.AllowSorting,
                grid.GridLines);
            await Task.WhenAll(
                JSRuntime.InvokeVoidAsync("window.localStorage.setItem", $"{value}-TSET", JsonSerializer.Serialize(gridSettings)).AsTask(),
                JSRuntime.InvokeVoidAsync("window.localStorage.setItem", $"{value}-OTSET", JsonSerializer.Serialize(ots)).AsTask()
            );
        }
    }
    public async Task SetGridSettings<TItem>(RadzenDataGrid<TItem> grid, Action<DataGridSettings?> assignSetting)
    {
        DataGridSettings? settings = await LoadStateAsync(grid);
        assignSetting(settings);
    }

    public DialogOptions GetDialogOptions(bool? withDialogHeader = null)
    {
        return new()
        {
            CloseDialogOnOverlayClick = true,
            CloseDialogOnEsc = true,
            Draggable = true,
            Resizable = true,
            ShowClose = withDialogHeader is not null ? !(bool)withDialogHeader : true
        };
    }

    public void PagingChanged<TItem>(RadzenDataGrid<TItem> grid, bool value)
    {
        if (grid is not null && value && (!string.IsNullOrEmpty(grid.Style) && grid.Style.Contains("max-height: 500px;")))
        {
            grid.Style = grid.Style.Replace("max-height: 500px;", "").Trim();
            grid.AllowVirtualization = true;
        }
        else if (grid is not null && !value && (string.IsNullOrEmpty(grid.Style) || !grid.Style.Contains("max-height: 500px;")))
        {
            grid.Style = $"{grid.Style} max-height: 500px;";
            grid.AllowVirtualization = false;
        }
    }

    async Task LoadSettingAsync(string key, Action<string> assignSetting)
    {
        var setting = await JSRuntime.InvokeAsync<string>("window.localStorage.getItem", key);
        assignSetting(setting);
    }

    public async Task UnsetGridSettings<TItem>(RadzenDataGrid<TItem> grid)
    {
        await Task.CompletedTask;

        if (grid is not null && grid.Attributes.TryGetValue("id", out var value))
        {
            await Task.WhenAll(
                JSRuntime.InvokeVoidAsync("window.localStorage.removeItem", $"{value}-TSET").AsTask(),
                JSRuntime.InvokeVoidAsync("window.localStorage.removeItem", $"{value}-OTSET").AsTask()
            );
        }
    }
}

class TableSettings : RadzenDataGrid<TableSettings>
{

}

class OtherTableSettings(
        FilterMode filterMode,
        bool allowPaging,
        bool allowColumnPicking,
        bool allowColumnResize,
        bool allowFiltering,
        bool allowSorting,
        DataGridGridLines gridLines
        )
{
    public FilterMode FilterMode { get; set; } = filterMode;
    public bool AllowPaging { get; set; } = allowPaging;
    public bool AllowColumnPicking { get; set; } = allowColumnPicking;
    public bool AllowColumnResize { get; set; } = allowColumnResize;
    public bool AllowFiltering { get; set; } = allowFiltering;
    public bool AllowSorting { get; set; } = allowSorting;
    public DataGridGridLines GridLines { get; set; } = gridLines;
}
