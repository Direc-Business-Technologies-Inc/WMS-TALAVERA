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
    public bool IsBusy { get; set; } = false;
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

    private QuickVirtualizedDropdown<LocationVM>? SourceLocationDropdown { get; set; }
    private QuickVirtualizedDropdown<LocationVM>? DestinationLocationDropdown { get; set; }
    private QuickVirtualizedDropdown<VendorVM>? VendorDropdown { get; set; }

    private List<TransferCategory> ReturnCategories = [.. TransferCategory.ReturnCategories];

    private static readonly SemaphoreSlim _concurrencySemaphore = new SemaphoreSlim(2, 2);

    const string PRINTABLE_URL_INTERCOMPANY = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1671&deploy=1&compid=11608969&ns-at=AAEJ7tMQ9evIwFEEUifIBokQgQ0jhowAItpfjv5Smu7B76K41lU&recordType=tranferOrder&isPickingTicket=true";
    const string PRINTABLE_URL_TO = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1671&deploy=1&compid=11608969&ns-at=AAEJ7tMQ9evIwFEEUifIBokQgQ0jhowAItpfjv5Smu7B76K41lU&recordType=tranferOrder&isPickingTicket=true";

    public string ReferenceString => string.IsNullOrEmpty(Model.ReferenceNumber) ? 
        ReadOnly ? "N/A" : "Auto-Generated" : 
        Model.ReferenceNumber;
    public string StatusString => Model.Status is null ?
        ReadOnly ? "N/A" : "To be submitted" :
        string.IsNullOrEmpty(Model.Status.Name) ? "---" : Model.Status.Name;

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
                QuantityOnHand = item.QuantityAvailable,
                QuantityAlloted = 0
            });
        }
        await LinesTable.DataGrid.Reload();
        await InvokeAsync(StateHasChanged);
    }

    async Task<(IEnumerable<LocationVM>, int)> SourceLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        await _concurrencySemaphore.WaitAsync();

        var result =  await LocationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);

        _concurrencySemaphore.Release();
        return result;
    }

    async Task<(IEnumerable<LocationVM>, int)> DestinationLocationProvider(DataGridIntent intent)
    {
        if (Model.ToSubsidiary is null) return ([], 0);

        await _concurrencySemaphore.WaitAsync();
        var result = await LocationHandler.GetLocationsBySubsidiaryAsync(intent, Model.ToSubsidiary.Id);

        _concurrencySemaphore.Release();
        return result;
    }

    async Task<(IEnumerable<VendorVM>, int)> VendorProvider(DataGridIntent intent)
    {
        if (Model.ToSubsidiary is null) return ([], 0);

        await _concurrencySemaphore.WaitAsync();
        var result = await VendorHandler.GetVendorsListBySubsidiaryAsync(intent, Model.ToSubsidiary.Id);

        _concurrencySemaphore.Release();
        return result;
    }

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {

        await _concurrencySemaphore.WaitAsync();
        var result = await SubsidiaryHandler.GetSubsidiariesAsync(intent);

        _concurrencySemaphore.Release();
        return result;
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitProvider(int itemId, DataGridIntent intent)
    {

        await _concurrencySemaphore.WaitAsync();
        var result = await ItemsHandler.GetItemUnits(itemId, intent);

        _concurrencySemaphore.Release();
        return result;
    }

    async Task OnSubsidiaryChanged(SubsidiaryVM? value)
    {
        var originalValue = Model.Subsidiary;
        Model.Subsidiary = value;

        if (Model.IsIntercompany && SameSubsidiary(value, Model.ToSubsidiary))
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

        Model.Lines.Clear();

        await OnLocationChanged(null);
        if (!Model.IsIntercompany)
        {
            await OnToSubsidiaryChanged(value);
        }
        SourceLocationDropdown?.Reset();
        await InvokeAsync(StateHasChanged);
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

        if (IsSameLocation(Model.SourceLocation, Model.DestinationLocation))
        {
            ToastService.Error("Source location may not be the same as the destination location");
            await Task.Yield();
            Model.DestinationLocation = originalValue;
            return;
        }

        Model.Lines.Clear();

        await InvokeAsync(StateHasChanged);
    }

    async Task OnDestinationLocationChanged(LocationVM? value)
    {
        var originalValue = Model.DestinationLocation;
        Model.DestinationLocation = value;

        if (IsSameLocation(Model.SourceLocation, Model.DestinationLocation))
        {
            ToastService.Error("Destination location may not be the same as the source location");
            await Task.Yield();
            Model.DestinationLocation = originalValue;
        }

        await InvokeAsync(StateHasChanged);
    }

    bool IsSameLocation(LocationVM? a, LocationVM? b)
    {
        return a is null || b is null ? false : a.Id == b.Id;
    }

    async Task OnToSubsidiaryChanged(SubsidiaryVM? value)
    {
        var originalValue = Model.ToSubsidiary;
        Model.ToSubsidiary = value;

        if (Model.IsIntercompany && SameSubsidiary(value, Model.Subsidiary))
        {
            ToastService.Warning("\"To Subsidiary\" cannot be the same as \"Subsidiary\"");
            await Task.Yield();
            Model.ToSubsidiary = originalValue;
            return;
        }

        Model.DestinationLocation = null;
        Model.Vendor = null;
        DestinationLocationDropdown?.Reset();
        VendorDropdown?.Reset();
    }

    async Task DeleteLine(StockTransferRequestLineVM line)
    {
        Model.Lines.Remove(line);
        await LinesTable.DataGrid.Reload();
    }

    async Task SetLineUoM(StockTransferRequestLineVM line, ItemUnitVM? uom)
    {
        decimal oldcr = line.UoM?.ConversionRate ?? 1;
        decimal newcr = uom?.ConversionRate ?? 1;

        line.QuantityAlloted *= oldcr / newcr;

        line.UoM = uom;
    }

    bool SameSubsidiary(SubsidiaryVM? a, SubsidiaryVM? b)
    {
        if (a is null && b is null) return false;
        return a?.Id == b?.Id;
    }

    string PrintableURL => Model.Category.IsInterCompany ? $"{PRINTABLE_URL_INTERCOMPANY}&recordId={Model.Id}" : $"{PRINTABLE_URL_TO}&recordId={Model.Id}";
        
    void Return()
    {
        if (!string.IsNullOrEmpty(ReturnURI)) NavManager.NavigateTo(ReturnURI, true);
    }

    void ActionClicked()
    {
        if (ReadOnly && !string.IsNullOrEmpty(ActionURI)) NavManager.NavigateTo(ActionURI, true);
    }
}
