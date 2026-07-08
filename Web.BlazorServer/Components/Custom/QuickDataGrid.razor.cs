using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using System.Reflection;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Custom.Utilities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Custom;

public partial class QuickDataGrid<TItem> : BaseComponent where TItem : class
{

    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Parameter] public string? Id { get; set; } = null;
    [Parameter] public string ActionName { get; set; } = GENERIC_ACTION_NAME;
    [Parameter] public RenderFragment? Columns { get; set; } = null;
    [Parameter] public RenderFragment? HeaderStart { get; set; } = null;
    [Parameter] public RenderFragment? HeaderEnd { get; set; } = null;
    [Parameter] public RenderFragment<TItem>? RowActions { get; set; } = null;
    [Parameter] public int? ActionWidth { get; set; } = null; // cringe
    [Parameter][EditorRequired] public required DataDelegate DataGetter { get; set; }
    [Parameter] public bool IgnorePageSettings { get; set; } = false;

    public const string GENERIC_ACTION_NAME = "Generic Action";
    public const string GENERIC_DATAGRID_ID = "generic_app_datagrid";
    AppDataGrid<TItem> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; }

    Dictionary<PropertyInfo, string> PropertyTitles = new();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Columns is null)
        {
            List<PropertyInfo> properties = [.. typeof(TItem).GetProperties().Where(prop =>
                !prop.IsDefined(typeof(QuickDataGridIgnore))
            )];
            PropertyTitles = properties.ToDictionary(
                x => x,
                x => x.GetCustomAttribute<QuickDataGridTitle>()?.Title ?? x.Name);
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    string GetFormat(PropertyInfo prop)
    {
        return prop.GetCustomAttribute<QuickDataGridStringFormat>()?.Format ?? "{0:d}";
    }

    async Task LoadGridSettings()
    {
        if (Id is null)
        {
            DataGridSettings = new();
        }
        else
        {
            await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
            if (IgnorePageSettings) DataGridSettings.CurrentPage = null;
        }
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<TItem>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionName, true);

            return await DataGetter(intent);

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionName));

        AppBusyService.SetBusy(ActionName, false);
        return DataGridResultVM<TItem>.New(action.Result.Data ?? [], action.Result.Count);
    }

    public delegate Task<(IEnumerable<TItem> Data, int Count)> DataDelegate(DataGridIntent intent);
}
