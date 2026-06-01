using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using System.Timers;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppTable<TItem> : BaseComponent where TItem : class
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;


    [Parameter] public string Id { get; init; } = $"{typeof(TItem).Name}-data-grid";
    [Parameter] public List<TItem> Data { get; set; } = new List<TItem>();
    [Parameter] public EventCallback<List<TItem>> DataChanged { get; set; }
    [Parameter] public Func<DataGridIntent, Task<DataGridResultVM<TItem>>>? DataGetter { get; set; }
    [Parameter] public RenderFragment HeaderTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    [Parameter] public RenderFragment Footer { get; set; }
    [Parameter] public RenderFragment LoadingTemplate { get; set; }
    [Parameter] public RenderFragment EmptyTemplate { get; set; }
    [Parameter] public RenderFragment Columns { get; set; }
    [Parameter] public DataGridSettings GridSettings { get; set; } = new();
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowDoubleClick { get; set; }
    [Parameter] public EventCallback<DataGridRowMouseEventArgs<TItem>> OnRowClick { get; set; }
    [Parameter] public EventCallback<TItem> OnRowSelect { get; set; }
    [Parameter] public EventCallback<TItem> OnRowDeselect { get; set; }
    [Parameter] public DataGridSelectionMode DataGridSelectionMode { get; set; } = DataGridSelectionMode.Single;
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public bool GridSettingsLoaded { get; set; } = false;
    [Parameter] public EventCallback<bool> GridSettingsLoadedChanged { get; set; }
    [Parameter] public IList<TItem> SelectedItems { get; set; } = new List<TItem>();
    [Parameter] public string ActionName { get; set; } = string.Empty;
    
    protected bool IsBusy => AppBusyService.IsBusy(ActionName);
    public RadzenDataGrid<TItem> DataGrid { get; set; } = default!;


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
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
