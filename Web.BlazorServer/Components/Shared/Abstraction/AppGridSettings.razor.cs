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
    [Parameter]
    public bool ReloadEntirePage { get; set; } = true;
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
    async Task SetPageSize(int value)
    {
        // The grid derives Skip from CurrentPage * PageSize. Changing the page size
        // while sitting on a later page therefore rewrites Skip without moving the
        // user: page 3 at size 10 (Skip=20) becomes Skip=40 at size 20, which
        // NetSuite rejects with 404 INVALID_PARAMETER ("offset out of bounds")
        // when the document no longer has that many rows.
        //
        // Assign the incoming value explicitly rather than relying on @bind-Value
        // ordering, then reset to the first page before reloading so Skip goes
        // back to 0. Assigning CurrentPage directly (rather than calling
        // FirstPage) keeps this to the single Reload that NotifyParent already
        // performs — FirstPage reloads internally, which would fire a second
        // data load.
        GridCopy.PageSize = value;
        GridCopy.CurrentPage = 0;
        await NotifyParent();
    }

    async Task SetPaging(bool value)
    {
        GridSettingsService.PagingChanged(GridCopy, value);
        // Toggling paging rescales Skip the same way a page-size change does,
        // so reposition to the first page before reloading.
        GridCopy.CurrentPage = 0;
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
        if (ReloadEntirePage)
            NavigationManager.Refresh(true);
        else
            Grid?.Reload();
        DialogService.Close();
    }
    #endregion Custom Functions

    #region Implements
    #endregion Implements

    #region Classes
    #endregion Classes

}
