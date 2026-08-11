using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Radzen.Blazor.Rendering;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnForm
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IVendorHandler vendorHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter][EditorRequired] public required SupplierReturnVM Model { get; set; }
    [Parameter][EditorRequired] public required EditContext EditContext { get; set; }

    [Parameter] public EventCallback<SupplierReturnVM> OnSecondaryAction { get; set; }
    [Parameter] public EventCallback<SupplierReturnVM> OnApprovalAction { get; set; }
    [Parameter] public EventCallback<SupplierReturnVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<SupplierReturnVM> OnReturn { get; set; }
    [Parameter] public string SecondaryActionString { get; set; } = "Action";
    [Parameter] public string SubmitString { get; set; } = "Submit";
    [Parameter] public string ReturnString { get; set; } = "Return";
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool Disabled { get; set; } = false;

    AppTable<SupplierReturnLineVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    QuickVirtualizedDropdown<LocationVM> LocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<SubsidiaryVM> SubsidiaryDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<PurchaseSubcategoryVM> PurchaseSubcategoryDropdown { get; set; } = default!;

    // need a linter
    readonly string ActionGetPO = "Get Purchase Order";
    bool canSelectPO = true;
    bool IsEditable => !ReadOnly && Model.SourcePO is null;
    bool IsFromPo => Model.SourcePO is not null;
    bool IsDisabled => Disabled || LoadingPO;
    bool LoadingPO = false;

    BarcodeStore BarcodeStore = new();

    readonly List<AppFilterDescriptor> ItemFilters = [
        DataGridFilterUtilities.GreaterThan("QuantityAvailable", 0)
    ];

    const string PRINTABLE_URL = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1671&deploy=1&compid=11608969&ns-at=AAEJ7tMQ9evIwFEEUifIBokQgQ0jhowAItpfjv5Smu7B76K41lU&recordType=vendorreturnauthorization&transactionDefault=true";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        canSelectPO = Model.SourcePO is null && Model.Id is null;
    }

    async Task<(IEnumerable<ReturnCategoryVM>, int)> CategoryProvider(DataGridIntent intent)
    {
        return await returnHandler.GetReturnCategories(intent);
    }

    async Task<(IEnumerable<LocationVM>, int)> LocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);
        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        if (Model.Vendor is null) return ([], 0);
        return await subsidiaryHandler.GetSubsidiariesByVendorAsync(intent, Model.Vendor.Id);
    }

    async Task<(IEnumerable<VendorVM>, int)> VendorProvider(DataGridIntent intent)
    {
        return await vendorHandler.GetVendorsListAsync(intent);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitsProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId,intent);
    }

    async Task<(IEnumerable<PurchaseCategoryVM>, int)> PurchaseCategoryProvider(DataGridIntent intent)
    {
        return await returnHandler.GetPurchaseCategoriesAsync(intent);
    }

    async Task<(IEnumerable<PurchaseSubcategoryVM>, int)> PurchaseSubcategoryProvider(DataGridIntent intent)
    {
        if (Model.PurchaseCategory is null) return ([], 0);
        return await returnHandler.GetPurchaseSubCategoriesAsync(Model.PurchaseCategory, intent);
    }

    async Task PurchaseCategorySet(PurchaseCategoryVM? val)
    {
        if (Model.PurchaseCategory == val) return;
        Model.PurchaseCategory = val;
        Model.PurchaseSubcategory = null;

        PurchaseSubcategoryDropdown.Reset();
    }

    async Task CategorySet(ReturnCategoryVM? val)
    {
        Model.ReturnCategory = val;
    }

    async Task SubsidiarySet(SubsidiaryVM? vm)
    {
        if (Model.Subsidiary == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing subsidiaries will remove all added items");
            if (!response) return;
        }

        Model.Subsidiary = vm;
        Model.Location = null;
        LocationDropdown.Reset();

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task LocationSet(LocationVM? vm)
    {
        if (Model.Location == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing locations will remove all added items");
            if (!response) return;
        }

        Model.Location = vm;
        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task VendorSet(VendorVM? vm)
    {
        if (Model.Vendor == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing vendors will remove all added items");
            if (!response) return;
        }

        Model.Vendor = vm;
        Model.Subsidiary = null;
        Model.Location = null;

        LocationDropdown.Reset();
        SubsidiaryDropdown.Reset();

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task GetPurchaseOrder(string poRef)
    {
        LoadingPO = true;
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            var x = await returnHandler.GetReturnFromPurchaseOrderAsync(poRef);
            return x;
        }, ActionGetPO);

        action.OnSuccess(async (po) =>
        {
            var prepBy = Model.PreparedBy;
            po.Adapt(Model);
            Model.CreatedFrom = po.ReferenceNumber;
            Model.Status = new ReturnStatusVM() { Name = "Pending Approval" };
            Model.PreparedBy = prepBy;
            Model.ReturnCategory = null;
            Model.Date = DateTime.Now;
            Model.Memo = $"Created via WMS from {po.ReferenceNumber}";
            canSelectPO = false;
        });

        LoadingPO = false;
        await InvokeAsync(StateHasChanged);
    }

    async Task ReturnClicked()
    {
        if (OnReturn.HasDelegate)
            await OnReturn.InvokeAsync(Model);
    }

    async Task SecondaryActionClicked()
    {
        if (OnSecondaryAction.HasDelegate)
            await OnSecondaryAction.InvokeAsync(Model);
    }

    async Task ApprovalAction()
    {
        if (OnApprovalAction.HasDelegate) await OnApprovalAction.InvokeAsync(Model);
    }

    async Task SubmitClicked()
    {
        if (OnSubmit.HasDelegate)
            await OnSubmit.InvokeAsync(Model);
    }

    async Task AddItems(List<ItemsVM> items)
    {
        if (Model.Location is null) return;

        canSelectPO = false;
        Model.Lines.AddRange(
            items.Select(x => new SupplierReturnLineVM
            {
                ItemId = x.Id,
                ItemCode = x.Name,
                ItemDescription = x.Description,
                UoM = x.StockUnit,
                Location = Model.Location,
                QuantityOnHand = x.QuantityOnHand,
                QuantityAvailable = x.QuantityAvailable,
                QuantityAlloted = 0
            }));

        await InvokeAsync(StateHasChanged);

        // Reload the table to display new items
        if (LinesTable?.DataGrid != null)
        {
            await LinesTable.DataGrid.Reload();
        }
    }

    async Task RemoveLine(SupplierReturnLineVM line)
    {
        Model.Lines.Remove(line);

        await InvokeAsync(StateHasChanged);
    }

    string PrintableURL => $"{PRINTABLE_URL}&recordId={Model.Id}";
    async Task LineUoMSet(SupplierReturnLineVM line, ItemUnitVM? uom)
    {
        var oldcr = line.UoM?.ConversionRate ?? 1;  
        var newcr = uom?.ConversionRate ?? 1;

        line.QuantityAlloted *= oldcr / newcr;
        line.UoM = uom;

        await InvokeAsync(StateHasChanged);
    }

    void ApplyBarcodes()
    {
        if (!BarcodeStore.Any()) return;

        foreach (var item in BarcodeStore.Items)
        {
            var itemCount = BarcodeStore.CountItemQuantity(item);
            //var itemLine = Model.Lines.First(x => x.ItemId == item.Id);

            //if (itemLine != null) itemLine.QuantityAlloted += itemCount / (itemLine.UoM?.ConversionRate ?? 1);

            SupplierReturnLineVM? itemLine;

            if (selectedItems.Any())
            {
                //itemLine = Model.Lines.FirstOrDefault(x => x.ItemId == selectedItems.First().ItemId && x.LineNumber == selectedItems.First().LineNumber);
                itemLine = Model.Lines[selectedItemIndex];
            }
            else
            {
                itemLine = Model.Lines.FirstOrDefault(x => x.ItemId == item.Id);
            }

            if (itemLine != null)
            {
                itemLine.QuantityAlloted += itemCount / (itemLine.UoM?.ConversionRate ?? 1);
            }
        }

        BarcodeStore.Clear();
    }

    private IList<SupplierReturnLineVM> selectedItems = new List<SupplierReturnLineVM>();
    private int selectedItemIndex { get; set; } = -1;

    async Task OnRowClick(DataGridRowMouseEventArgs<SupplierReturnLineVM> args)
    {
        if (selectedItems.Contains(args.Data))
        {
            selectedItems = new List<SupplierReturnLineVM>();       // Unselect
            selectedItemIndex = -1;
        }
        else
        {
            selectedItems = new List<SupplierReturnLineVM>();
            selectedItems = new List<SupplierReturnLineVM> { args.Data }; // Select
            selectedItemIndex = Model.Lines.IndexOf(args.Data);
        }
    }

    bool IsValidBarcode(BarcodeVM barcode, out string reason)
    {
        var line = Model.Lines.FirstOrDefault(x => x.ItemId == barcode.Item?.Id && barcode.UoM?.Id == x.UoM?.Id && barcode.UoM is not null) ??
            Model.Lines.FirstOrDefault(x => x.ItemId == barcode.Item?.Id);

        if (selectedItems.Count != 0)
        {
            //line = selectedItems.FirstOrDefault(x => x.ItemId == barcode.Item?.Id);
            line = Model.Lines[selectedItemIndex];

            if (line.ItemId != barcode.Item?.Id)
            {
                reason = $"The item {barcode.Item?.ItemNumber} does not match the selected item {line.ItemCode}";
                return false;
            }
        }

        if (line is null)
        {
            reason = $"The item {barcode.Item?.ItemNumber} does not exist in the current document";
            return false;
        }

        var uomRate = line.UoM?.ConversionRate ?? 1;
        var itemCount = BarcodeStore.CountItemQuantity(line.ItemId) / uomRate;
        var incomingCount = (barcode.UoM?.ConversionRate ?? 0) / uomRate;

        if (line.QuantityOnHandByUoM - line.QuantityAlloted - itemCount < incomingCount)
        {
            reason = $"The quantity of the item {line.ItemCode} exceeds the expected amount";
            return false;
        }

        reason = "";
        return true;
    }
}
