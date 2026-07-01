using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components;

public partial class STRForm
{
    [Parameter]
    [EditorRequired]
    public StockTransferRequestInfoVM Model { get; set; } = new();
    [Parameter]
    public Func<StockTransferRequestInfoVM, Task<bool>>? OnSubmit { get; set; }
    [Parameter]
    public EditContext? EditContext { get; set; }
    [Parameter]
    public bool ReadOnly { get; set; } = false;
    [Parameter]
    public string? ReturnURI { get; set; }
    [Parameter]
    public string? ActionURI { get; set; }
    [Parameter]
    public string ActionLabel { get; set; } = "Submit";
    [Parameter]
    public string ReturnLabel { get; set; } = "Return";

    [Inject]
    IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject]
    ILocationHandler LocationHandler { get; set; } = default!;
    [Inject]
    ISubsidiaryHandler SubsidiaryHandler { get; set; } = default!;
    [Inject]
    IVendorHandler VendorHandler { get; set; } = default!;
    [Inject]
    IItemsHandler ItemsHandler { get; set; } = default!;

    AppTable<StockTransferRequestLineVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    readonly string ActionGetLocations = "Get Locations";
    readonly string ActionGetSubsidiaries = "Get Subsidiaries";
    readonly string ActionGetVendors = "Get Vendors";
    readonly string ActionGetItemUnits = "Get Item Units";

    private QuickVirtualizedDropdown<LocationVM> SourceLocationDropdown { get; set; } = default!;
    private QuickVirtualizedDropdown<LocationVM> DestinationLocationDropdown { get; set; } = default!;
    private QuickVirtualizedDropdown<VendorVM>? VendorDropdown { get; set; }

    private List<TransferCategory> ReturnCategories = [.. TransferCategory.ReturnCategories];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await LoadGridSettings();
        }
    }

    async Task LoadGridSettings()
    {
        if (GridSettingsLoaded) return;

        await GridSettingsService.SetGridSettings(LinesTable.DataGrid, settings => TableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await LinesTable.DataGrid.ReloadSettings();
        await LinesTable.DataGrid.Reload();
    }

    async Task HandleSubmit()
    {
        if (Model.Lines.Count == 0)
        {
            await DialogService.Alert("Please add at least one item", "Error");
            return;
        }

        bool success = true;
        if (OnSubmit is not null) success = await OnSubmit(Model);
        if (success && !string.IsNullOrEmpty(ActionURI))
        {
            NavManager.NavigateTo(ActionURI, true);
        }
    }

    async Task AddItems(List<ItemsVM> items)
    {
        foreach (var item in items)
        {
            Model.Lines.Add(new()
            {
                ItemId = item.Id,
                ItemCode = item.ItemNumber,
                ItemDescription = item.Name,
                Warehouse = Model.SourceLocation?.Name ?? string.Empty,
                UoM = item.StockUnit,
                QuantityOnHand = item.QuantityOnHand,
                QuantityAlloted = 0
            });
        }
        await LinesTable.DataGrid.Reload();
        await InvokeAsync(StateHasChanged);
    }

    async Task<(IEnumerable<LocationVM>, int)> SourceLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await LocationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<LocationVM>, int)> DestinationLocationProvider(DataGridIntent intent)
    {
        if (Model.ToSubsidiary is null) return ([], 0);

        return await LocationHandler.GetLocationsBySubsidiaryAsync(intent, Model.ToSubsidiary.Id);
    }

    async Task<(IEnumerable<VendorVM>, int)> VendorProvider(DataGridIntent intent)
    {
        if (Model.ToSubsidiary is null) return ([], 0);

        return await VendorHandler.GetVendorsListBySubsidiaryAsync(intent, Model.ToSubsidiary.Id);
    }

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await SubsidiaryHandler.GetSubsidiariesAsync(intent);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitProvider(int itemId, DataGridIntent intent)
    {
        return await ItemsHandler.GetItemUnits(itemId, intent);
    }

    async Task OnSubsidiaryChanged(SubsidiaryVM? value)
    {
        var originalValue = Model.Subsidiary;
        Model.Subsidiary = value;

        if (SameSubsidiary(value, Model.ToSubsidiary))
        {
            ToastService.Warning("\"Subsidiary\" cannot be the same as \"To Subsidiary\"");
            await Task.Yield();
            Model.Subsidiary = originalValue;
            return;
        }

        if (Model.Lines.Any())
        {
            var confirm = await DialogService.Confirm(message: "Changing subsidiaries will clear added items") ?? false;
            if (!confirm)
            {
                await Task.Yield();
                Model.Subsidiary = originalValue;
                return;
            }
        }
        if (value != Model.Subsidiary)
        {
            Model.Lines.Clear();
            Model.Subsidiary = value;
            Model.SourceLocation = null;
            if (!Model.IsIntercompany)
            {
                Model.ToSubsidiary = value;
                Model.DestinationLocation = null;
                DestinationLocationDropdown.Reset();
            }
            SourceLocationDropdown.Reset();
            await InvokeAsync(StateHasChanged);
        }
    }


    async Task OnLocationChanged(LocationVM? value)
    {
        var originalValue = Model.SourceLocation;
        Model.SourceLocation = value;

        if (Model.Lines.Any())
        {
            var confirm = await DialogService.Confirm(message: "Changing source warehouse will clear added items") ?? false;
            if (!confirm)
            {
                await Task.Yield();
                Model.SourceLocation = originalValue;
                return;
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    async Task OnToSubsidiaryChanged(SubsidiaryVM? value)
    {
        var originalValue = Model.ToSubsidiary;
        Model.ToSubsidiary = value;

        if (SameSubsidiary(value, Model.Subsidiary))
        {
            ToastService.Warning("\"To Subsidiary\" cannot be the same as \"Subsidiary\"");
            await Task.Yield();
            Model.ToSubsidiary = originalValue;
            return;
        }

        Model.ToSubsidiary = value;
        Model.DestinationLocation = null;
        Model.Vendor = null;
        DestinationLocationDropdown.Reset();
        VendorDropdown?.Reset();
    }

    async Task DeleteLine(StockTransferRequestLineVM line)
    {
        Model.Lines.Remove(line);
        await LinesTable.DataGrid.Reload();
    }

    bool SameSubsidiary(SubsidiaryVM? a, SubsidiaryVM? b)
    {
        if (a is null && b is null) return false;
        return a?.Id == b?.Id;
    }

    void Return()
    {
        if (!string.IsNullOrEmpty(ReturnURI)) NavManager.NavigateTo(ReturnURI, true);
    }

    void ActionClicked()
    {
        if (ReadOnly && !string.IsNullOrEmpty(ActionURI)) NavManager.NavigateTo(ActionURI, true);
    }
}
