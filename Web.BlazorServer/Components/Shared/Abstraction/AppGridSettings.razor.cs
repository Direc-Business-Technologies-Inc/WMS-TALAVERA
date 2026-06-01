using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Web.BlazorServer.Components.Shared.CascadingValues;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Components.Shared.Abstraction;

[CascadingTypeParameter(nameof(TItem))]
public partial class AppGridSettings<TItem>
{
    #region Parameters
    [Parameter]
    public required RadzenDataGrid<TItem> Grid { get; set; }

    [Parameter]
    public EventCallback<RadzenDataGrid<TItem>> GridChanged { get; set; }

    [Parameter]
    public required DataGridSettings GridSettings { get; set; }

    [Parameter]
    public EventCallback<DataGridSettings> GridSettingsChanged { get; set; }

    [Parameter]
    public EventCallback<RadzenDataGrid<TItem>> Change { get; set; }

    [Parameter]
    public RenderFragment? AdditionalSettings { get; set; }

    [Parameter]
    public bool DisableAdvanceSettings { get; set; } = false;
    #endregion Parameters

    #region Cascading Parameters
    [CascadingParameter]
    HasUnsavedChangesProvider HasUnsavedChangesProvider { get; set; } = default!;
    #endregion Cascading Parameters

    #region Injects
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IGridSettingsService GridSettingsService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;
    #endregion Injects

    #region Data Structures
    List<FilterMode> FilterModes { get; set; } = [.. Array.AsReadOnly(Enum.GetValues<FilterMode>()).Where(s => s != FilterMode.CheckBoxList)];
    List<DataGridGridLines> GridLines { get; set; } = [.. Array.AsReadOnly(Enum.GetValues<DataGridGridLines>())];
    #endregion Data Structures

    #region Custom Classes
    #endregion Custom Classes

    #region Primitives
    bool ShowAdvanceSettings { get; set; }
    #endregion Primitives

    #region Radzens
    RadzenDataGrid<TItem> GridCopy { get; set; } = new();
    #endregion Radzens

    #region Overrides
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Reset();
            StateHasChanged();
        }
        base.OnAfterRender(firstRender);
    }
    #endregion Overrides

    #region Custom Functions
    async Task SetPaging(bool value)
    {
        GridSettingsService.PagingChanged(GridCopy, value);
        await NotifyParent();
    }

    async Task NotifyParent()
    {
        await GridSettingsService.SaveStateAsync(GridCopy, GridCopy.Settings);
        await GridChanged.InvokeAsync(GridCopy);
        await GridSettingsChanged.InvokeAsync(GridCopy.Settings);
        await Change.InvokeAsync(GridCopy);
        await GridCopy.Reload();
    }

    void Reset()
    {
        GridCopy = Grid;
    }

    void RefreshPage()
    {
        NavigationManager.Refresh(true);
        DialogService.Close();
    }
    #endregion Custom Functions

    #region Implements
    #endregion Implements

    #region Classes
    #endregion Classes

}
