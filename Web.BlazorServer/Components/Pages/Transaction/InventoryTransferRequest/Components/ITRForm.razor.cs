using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class ITRForm
{
    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ICustomerHandler customerHandler { get; set; } = default!;
    [Inject] IInventoryHandler inventoryHandler { get; set; } = default!;
    [Parameter][EditorRequired] public InventoryTransferRequestVM Model { get; set; }
    [Parameter][EditorRequired] public EditContext EditContext { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnReturn { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSecondaryAction { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnApprovalAction { get; set; }
    [Parameter] public string ReturnLabel { get; set; } = "Return";
    [Parameter] public string SubmitLabel { get; set; } = "Submit";
    [Parameter] public string SecondaryActionLabel { get; set; } = "Action";
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool EditMode { get; set; } = false;
    [Parameter] public bool Disabled { get; set; } = false;

    AppTable<InventoryTransferRequestLineVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    QuickVirtualizedDropdown<LocationVM> SourceLocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<LocationVM> DestinationLocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<SubsidiaryVM> SubsidiaryDropdown { get; set; } = default!;

    BarcodeStore BarcodeStore = new();

    HashSet<int> LoadedInventoryDetails = new();

    readonly List<AppFilterDescriptor> ItemFilters = [
        DataGridFilterUtilities.GreaterThan("QuantityOnHand", 0)
    ];

    bool _isBusy = false;
    bool IsDisabled => Disabled || _isBusy;

    async Task<(IEnumerable<CustomerVM>, int)> CustomerProvider(DataGridIntent intent)
    {
        return await customerHandler.GetCustomersListAsync(intent);
    }
    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await subsidiaryHandler.GetSubsidiariesAsync(intent);
    }
    async Task<(IEnumerable<LocationVM>, int)> SourceLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }
    async Task<(IEnumerable<LocationVM>, int)> DestinationLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }
    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId, intent);
    }

    async Task AddItems(List<ItemsVM> items)
    {
        Model.Lines.AddRange(items.Select(x => new InventoryTransferRequestLineVM
        {
            ItemID = x.Id,
            ItemCode = x.Name,
            ItemDescription = x.Description,
            UsesBins = x.UsesBins,
            UoM = x.StockUnit,
            QuantityAvailable = x.QuantityAvailable,
            QuantityOnHand = x.QuantityOnHand,
            Location = Model.SourceLocation
        }));

        await InvokeAsync(StateHasChanged);

        // Reload the table to display new items
        if (LinesTable?.DataGrid != null)
        {
            await LinesTable.DataGrid.Reload();
        }
    }
    async Task SecondaryAction()
    {
        if (OnSecondaryAction.HasDelegate) await OnSecondaryAction.InvokeAsync(Model);
    }

    async Task ApprovalAction()
    {
        if (OnApprovalAction.HasDelegate) await OnApprovalAction.InvokeAsync(Model);
    }

    async Task Return()
    {
        if (OnReturn.HasDelegate) await OnReturn.InvokeAsync(Model);
    }

    async Task LoadInventoryDetails(InventoryTransferRequestLineVM line)
    {
        if (Model.Id == 0)
            return;
        if (line.LineNumber is null || line.SourceLine is null)
            return;
        if (LoadedInventoryDetails.Contains((int)line.SourceLine))
            return;

        _isBusy = true;

        var details = await inventoryHandler.GetInventoryDetails(Model.Id, (int)line.SourceLine);

        foreach (var item in details)
        {
            item.QuantityAlloted = -item.QuantityAlloted; // quantity will be negative since it is outgoing
        }
        line.InventoryDetails.AddRange(details);
        LoadedInventoryDetails.Add((int)line.SourceLine);
        await MarkLineDirty(line);

        _isBusy = false;
    }

    async Task MarkLineDirty(InventoryTransferRequestLineVM line) 
    {
        line.LineNumber = null;
        await InvokeAsync(StateHasChanged);
    }

    async Task Submit()
    {
        if (OnSubmit.HasDelegate) await OnSubmit.InvokeAsync(Model);
    }
    async Task SubsidiarySet(SubsidiaryVM? value)
    {
        var oldValue = Model.Subsidiary;
        Model.Subsidiary = value;
        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing subsidiaries will clear added items", "Change Subsidiaries?");
            await Task.Yield();
            Model.Subsidiary = oldValue;
            if (!response) return;
        }

        Model.Lines.Clear();

        await Task.WhenAll(
            LocationSet(null),
            DestinationLocationSet(null)
        );


        SourceLocationDropdown.Reset();
        DestinationLocationDropdown.Reset();

        await InvokeAsync(StateHasChanged);
    }

    async Task RemoveLine(InventoryTransferRequestLineVM line)
    {
        Model.Lines.Remove(line);

        await InvokeAsync(StateHasChanged);
    }

    async Task LocationSet(LocationVM? value)
    {
        var oldValue = Model.SourceLocation;
        Model.SourceLocation = value;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing source location will clear added items", "Change Source Location?");
            if (!response)
            {
                await Task.Yield();
                Model.SourceLocation = oldValue;
                return;
            }
        }

        if (_areEqual(Model.DestinationLocation, value))
        {
            ToastService.Error("Destination warehouse cannot be the same as the source warehouse");
            await Task.Yield();
            Model.SourceLocation = oldValue;
            return;
        }

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task DestinationLocationSet(LocationVM? value)
    {
        var oldValue = Model.DestinationLocation;
        Model.DestinationLocation = value;

        if (_areEqual(Model.SourceLocation, value))
        {
            ToastService.Error("Destination warehouse cannot be the same as the source warehouse");
            await Task.Yield();
            Model.DestinationLocation = oldValue;
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    async Task SetLineUoM(InventoryTransferRequestLineVM line, ItemUnitVM? uom)
    {
        var oldUoM = line.UoM;
        line.UoM = uom;

        if (line.LineNumber != null)
        {
            var prompt = await AlertService.PromptAsync("Changing the item unit will clear the inventory details");
            if (!prompt)
            {
                line.UoM = oldUoM;
                await InvokeAsync(StateHasChanged);
                return;
            }

            line.InventoryDetails.Clear();
        }

        await MarkLineDirty(line);
        decimal oldcr = oldUoM?.ConversionRate ?? 1;
        decimal newcr = uom?.ConversionRate ?? 1;

        line.QuantityAlloted *= oldcr / newcr;
        line.InventoryDetails.ForEach(x => x.QuantityAlloted *= oldcr / newcr);

        await InvokeAsync(StateHasChanged);
    }

    async Task SetLineQuantity(InventoryTransferRequestLineVM line, decimal amount)
    {
        var oldAmount = line.QuantityAlloted;
        line.QuantityAlloted = amount;

        if (line.InventoryDetails.Count > 0 || line.LineNumber is not null) 
        {
            var prompt = await AlertService.PromptAsync("Changing quantity will clear inventory details");
            if (!prompt)
            {
                line.QuantityAlloted = oldAmount;
                await InvokeAsync(StateHasChanged);
                return;
            }
        }

        line.InventoryDetails.Clear();
        await MarkLineDirty(line);
    }

    void ApplyBarcodes()
    {
        if (!BarcodeStore.Any()) return;

        foreach (var item in BarcodeStore.Items)
        {
            var itemCount = BarcodeStore.CountItemQuantity(item);
            InventoryTransferRequestLineVM? itemLine;

            if (selectedItems.Any())
            {
                //itemLine = Model.Lines.FirstOrDefault(x => x.ItemId == selectedItems.First().ItemId && x.LineNumber == selectedItems.First().LineNumber);
                itemLine = Model.Lines[selectedItemIndex];
            }
            else
            {
                itemLine = Model.Lines.FirstOrDefault(x => x.ItemID == item.Id);
            }

            if (itemLine != null)
            {
                itemLine.QuantityAlloted += itemCount / (itemLine.UoM?.ConversionRate ?? 1);
            }
        }

        BarcodeStore.Clear();
    }

    private IList<InventoryTransferRequestLineVM> selectedItems = new List<InventoryTransferRequestLineVM>();
    private int selectedItemIndex { get; set; } = -1;

    async Task OnRowClick(DataGridRowMouseEventArgs<InventoryTransferRequestLineVM> args)
    {
        if (selectedItems.Contains(args.Data))
        {
            selectedItems = new List<InventoryTransferRequestLineVM>();       // Unselect
            selectedItemIndex = -1;
        }
        else
        {
            selectedItems = new List<InventoryTransferRequestLineVM>();
            selectedItems = new List<InventoryTransferRequestLineVM> { args.Data }; // Select
            selectedItemIndex = Model.Lines.IndexOf(args.Data);
        }
    }

    bool IsValidBarcode(BarcodeVM barcode, out string reason)
    {
        var line = Model.Lines.FirstOrDefault(x => x.ItemID == barcode.Item?.Id && barcode.UoM?.Id == x.UoM?.Id && barcode.UoM is not null) ??
            Model.Lines.FirstOrDefault(x => x.ItemID == barcode.Item?.Id);

        if (selectedItems.Count != 0)
        {
            //line = selectedItems.FirstOrDefault(x => x.ItemId == barcode.Item?.Id);
            line = Model.Lines[selectedItemIndex];
        }

        if (line is null)
        {
            reason = $"The item {barcode.Item?.ItemNumber} does not exist in the current document";
            return false;
        }

        var uomRate = line.UoM?.ConversionRate ?? 1;
        var itemCount = BarcodeStore.CountItemQuantity(line.ItemID) / uomRate;
        var incomingCount = (barcode.UoM?.ConversionRate ?? 0) / uomRate;

        if (line.QuantityOnHandByUoM - line.QuantityAlloted - itemCount < incomingCount)
        {
            reason = $"The quantity of the item {line.ItemCode} exceeds the expected amount";
            return false;
        }

        reason = "";
        return true;
    }

    bool _areEqual(LocationVM? a, LocationVM? b) => (a is null || b is null) ? false : a?.Id == b?.Id;
}
