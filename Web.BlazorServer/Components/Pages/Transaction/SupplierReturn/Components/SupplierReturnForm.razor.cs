using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Radzen.Blazor.Rendering;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnForm
{
    SubmitOnce SubmitOnce { get; } = new();
    bool IsSubmitting => SubmitOnce.IsSubmitting;
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
    [Parameter] public bool EditMode { get; set; } = false;

    AppTable<SupplierReturnLineVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    private List<ItemsVM> Items = new();

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

    const string VendorSearchProperty = nameof(VendorVM.ReferenceNumber);

    readonly List<AppFilterDescriptor> ItemFilters = [];

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

    async Task<(IEnumerable<ItemsVM>, int)> LocationItemsProvider()
    {
        var itemIds = Model.Lines.Select(x => x.ItemId).Distinct().ToList();

        DataGridIntent intent = new DataGridIntent
        {
            Filters = [
                DataGridFilterUtilities.In(nameof(ItemsVM.Id), itemIds)
                ],

            Take = 1000
        };

        int location = Model.Location.Id;

        var result = location == 0 ?
        await itemsHandler.GetItemsDataGridAsync(intent) :
        await itemsHandler.GetItemsAtLocationDataGridAsync(intent, location, Model.Subsidiary?.Id);

        Items = result.Data.ToList();

        return result;
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
        BarcodeStore.Clear();
        selectedItems.Clear();
        selectedItemIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    async Task LocationSet(LocationVM? vm)
    {
        if (Model.Location == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing the locations may remove items that are not present at the selected location");
            if (!response) return;
        }

        Model.Location = vm;
        BarcodeStore.Clear();
        selectedItems.Clear();
        selectedItemIndex = -1;
        //Model.Lines.Clear();

        if (Model.Location is null) return;
        if (Model.Lines == null || Model.Lines.Count == 0) return;

        await LocationItemsProvider();

        var itemsById = Items.ToDictionary(x => x.Id);

        Model.Lines.RemoveAll(line => !itemsById.ContainsKey(line.ItemId));

        foreach (var line in Model.Lines)
        {
            var item = itemsById[line.ItemId];

            line.Location = Model.Location;
            line.QuantityOnHand = item.QuantityOnHand;
            line.QuantityAvailable = item.QuantityAvailable;
        }

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
        BarcodeStore.Clear();
        selectedItems.Clear();
        selectedItemIndex = -1;
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
        if (!TryValidateOnHand(out var quantityError))
        {
            ToastService.Error(quantityError, "Invalid Quantity");
            return;
        }

        if (OnApprovalAction.HasDelegate) await OnApprovalAction.InvokeAsync(Model);
    }

    Task SubmitClicked() => SubmitOnce.RunAsync(SubmitCore);

    async Task SubmitCore()
    {
        if (!TryValidateOnHand(out var quantityError))
        {
            ToastService.Error(quantityError, "Invalid Quantity");
            return;
        }

        // Per-line Preferred Vendor is required on every line (posted to
        // custcol_dbti_vendor). Name the offending lines so the user can find
        // them; per-row RadzenRequiredValidator cannot work here because grid
        // rows share one column template.
        var linesMissingVendor = Model.Lines
            .Select((line, index) => (line, index))
            .Where(x => x.line.Vendor is null)
            .Select(x => (x.line.LineNumber ?? x.index + 1).ToString())
            .ToList();
        if (linesMissingVendor.Count > 0)
        {
            ToastService.Error($"Preferred Vendor is required on line(s): {string.Join(", ", linesMissingVendor)}", "Error");
            return;
        }

        if (OnSubmit.HasDelegate)
            await OnSubmit.InvokeAsync(Model);
    }

    bool TryValidateOnHand(out string error)
    {
        var invalidLineNumbers = new HashSet<string>();
        foreach (var (line, index) in Model.Lines.Select((line, index) => (line, index)))
        {
            if (line.QuantityAlloted < 0 || line.UoM is { ConversionRate: <= 0 })
                invalidLineNumbers.Add(line.LineNumber?.ToString() ?? (index + 1).ToString());
        }

        foreach (var sameItemLines in Model.Lines.GroupBy(line => line.ItemId))
        {
            var totalAllotedInBaseUnits = sameItemLines.Sum(line =>
                line.QuantityAlloted * (line.UoM?.ConversionRate > 0 ? line.UoM.ConversionRate : 1m));
            var quantityOnHand = sameItemLines.First().QuantityOnHand;
            if (totalAllotedInBaseUnits > quantityOnHand)
            {
                foreach (var (line, index) in sameItemLines.Select(line => (line, Model.Lines.IndexOf(line))))
                    invalidLineNumbers.Add(line.LineNumber?.ToString() ?? (index + 1).ToString());
            }
        }

        error = invalidLineNumbers.Count == 0
            ? string.Empty
            : $"Total allotted quantity must be between 0 and On Hand on line(s): {string.Join(", ", invalidLineNumbers)}.";
        return invalidLineNumbers.Count == 0;
    }

    decimal GetMaxAllotedQuantity(SupplierReturnLineVM line)
    {
        var conversionRate = line.UoM?.ConversionRate > 0 ? line.UoM.ConversionRate : 1m;
        var otherLinesBaseQuantity = Model.Lines
            .Where(otherLine => otherLine.ItemId == line.ItemId && !ReferenceEquals(otherLine, line))
            .Sum(otherLine => otherLine.QuantityAlloted * (otherLine.UoM?.ConversionRate ?? 1m));

        return Math.Max(0, (line.QuantityOnHand - otherLinesBaseQuantity) / conversionRate);
    }

    async Task AddItems(List<ItemsVM> items)
    {
        var selectedVendor = Model.Vendor;
        if (Model.Location is null || selectedVendor is null) return;

        var eligibleItems = items
            .Where(item => item.PreferredVendorId == selectedVendor.Id)
            .ToList();
        if (eligibleItems.Count != items.Count)
        {
            ToastService.Error("Some selected items are not under the selected preferred vendor.", "Invalid Item");
        }
        if (eligibleItems.Count == 0) return;

        canSelectPO = false;

        // New lines use the selected header vendor. Clone it per row so editing
        // one line can never mutate the others.
        var headerVendor = new VendorVM
        {
            Id = selectedVendor.Id,
            ReferenceNumber = selectedVendor.ReferenceNumber,
            Name = selectedVendor.Name,
            CompanyName = selectedVendor.CompanyName,
            Category = selectedVendor.Category
        };
        Model.Lines.AddRange(
            eligibleItems.Select(x => new SupplierReturnLineVM
            {
                ItemId = x.Id,
                ItemCode = x.Name,
                ItemDescription = x.Description,
                UoM = x.StockUnit,
                Vendor = new VendorVM
                {
                    Id = headerVendor.Id,
                    ReferenceNumber = headerVendor.ReferenceNumber,
                    Name = headerVendor.Name,
                    CompanyName = headerVendor.CompanyName,
                    Category = headerVendor.Category
                },
                Location = Model.Location,
                QuantityOnHand = x.QuantityOnHand,
                QuantityAvailable = x.QuantityAvailable,
                QuantityAlloted = 0
            }));

        if (!TryValidateOnHand(out var quantityError))
        {
            ToastService.Error(quantityError, "Invalid Quantity");
        }

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
        if (selectedItems.Any(x => x.ItemId == line.ItemId))
        {
            selectedItemIndex = -1;
            selectedItems.Clear();
        }
        await InvokeAsync(StateHasChanged);
    }

    string? PrintableURL => NetSuitePrint.VendorReturn(Model.Id);
    async Task LineUoMSet(SupplierReturnLineVM line, ItemUnitVM? uom)
    {
        var oldcr = line.UoM?.ConversionRate ?? 1;  
        var newcr = uom?.ConversionRate ?? 1;

        line.QuantityAlloted *= oldcr / newcr;
        line.UoM = uom;

        await InvokeAsync(StateHasChanged);
    }

    async Task LineVendorSet(SupplierReturnLineVM line, VendorVM? vendor)
    {
        line.Vendor = vendor;
        await InvokeAsync(StateHasChanged);
    }

    async void ApplyBarcodes()
    {
        if (!BarcodeStore.Any()) return;

        foreach (var barcode in BarcodeStore.Barcodes)
        {
            decimal baseItemCount = BarcodeStore.CountItemQuantityPerBarcode(barcode);
            if (baseItemCount == 0) continue;

            SupplierReturnLineVM? itemLine;

            if (selectedItems.Any())
            {
                //itemLine = Model.Lines.FirstOrDefault(x => x.ItemId == selectedItems.First().ItemId && x.LineNumber == selectedItems.First().LineNumber);
                itemLine = Model.Lines[selectedItemIndex];
            }
            else
            {
                itemLine = Model.Lines.FirstOrDefault(x => x.ItemId == barcode.Item.Id);
            }

            if (itemLine != null)
            {
                // Convert base count to the line's specific UOM rate
                var lineConversionRate = itemLine.UoM?.ConversionRate ?? 1;
                itemLine.QuantityAlloted += baseItemCount / lineConversionRate;
            }
            else
            {
                var item = barcode.Item;

                // Default new lines to the item's StockUnit (Base UOM)
                var targetUom = barcode.UoM;
                var lineConversionRate = targetUom?.ConversionRate ?? 1;

                // Same default as AddItems: the RTS header vendor wins; the item
                // master default is the fallback when no header vendor is set.
                VendorVM? lineVendor = Model.Vendor is null
                    ? item.PreferredVendorId.HasValue ? new VendorVM { Id = item.PreferredVendorId.Value, Name = item.PreferredVendorName ?? string.Empty } : null
                    : new VendorVM
                    {
                        Id = Model.Vendor.Id,
                        ReferenceNumber = Model.Vendor.ReferenceNumber,
                        Name = Model.Vendor.Name,
                        CompanyName = Model.Vendor.CompanyName,
                        Category = Model.Vendor.Category
                    };
                Model.Lines.Add(new SupplierReturnLineVM
                {
                    ItemId = item.Id,
                    ItemCode = item.ItemNumber,
                    ItemDescription = item.Name,
                    UoM = targetUom,
                    Vendor = lineVendor,
                    Location = Model.Location,
                    QuantityOnHand = item.QuantityOnHand,
                    QuantityAvailable = item.QuantityAvailable,
                    QuantityAlloted = baseItemCount / lineConversionRate
                });
            }
        }

        BarcodeStore.Clear();

        await InvokeAsync(StateHasChanged);

        // Reload the table to display new items
        if (LinesTable?.DataGrid != null)
        {
            await LinesTable.DataGrid.Reload();
        }
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
        if (Model.Vendor is null || barcode.Item?.PreferredVendorId != Model.Vendor.Id)
        {
            reason = "This item is not under the selected preferred vendor.";
            return false;
        }

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

        if (line is null && barcode.Item is null)
        {
            reason = "The scanned barcode has no item data.";
            return false;
        }

        var incomingBaseQuantity = barcode.UoM?.ConversionRate ?? 0;
        var itemId = barcode.Item?.Id ?? line!.ItemId;
        var alreadyScannedBaseQuantity = BarcodeStore.CountItemQuantity(itemId);
        var allocatedBaseQuantity = Model.Lines
            .Where(existingLine => existingLine.ItemId == itemId)
            .Sum(existingLine => existingLine.QuantityAlloted * (existingLine.UoM?.ConversionRate ?? 1));
        var onHand = line?.QuantityOnHand ?? barcode.Item!.QuantityOnHand;
        if (allocatedBaseQuantity + alreadyScannedBaseQuantity + incomingBaseQuantity > onHand)
        {
            reason = $"The scan would exceed On Hand for item {barcode.Item?.ItemNumber ?? line?.ItemCode}.";
            return false;
        }

        reason = "";
        return true;
    }
}
