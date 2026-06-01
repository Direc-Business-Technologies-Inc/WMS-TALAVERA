using Radzen;
using Radzen.Blazor;

namespace Web.BlazorServer.Services.Repositories;

public interface IGridSettingsService
{
    public Task<DataGridSettings?> LoadStateAsync<T>(RadzenDataGrid<T> grid);
    public Task SetGridSettings<TItem>(RadzenDataGrid<TItem> grid, Action<DataGridSettings?> assignSetting);
    public Task UnsetGridSettings<TItem>(RadzenDataGrid<TItem> grid);
    public Task SaveStateAsync<TItem>(RadzenDataGrid<TItem> grid, DataGridSettings gridSettings);
    public Task<DataGridSettings> GridSettingsChanged<TItem>(RadzenDataGrid<TItem> grid, DataGridSettings newSettings);
    public DialogOptions GetDialogOptions(bool? withDialogHeader = null);
    public void PagingChanged<TItem>(RadzenDataGrid<TItem> grid, bool value);
}
