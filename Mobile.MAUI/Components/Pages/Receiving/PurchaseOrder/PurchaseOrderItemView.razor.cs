using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.PurchaseOrder;
using System.Text.Json;
using static Mobile.MAUI.Components.Reusables.WeightOptionDialog;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.Helpers.FormatHelper;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    AppAction<List<PurchaseOrderLineVM>> ActionGetPOItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<PurchaseOrderLineVM> GoodPOItems = [];
    List<PurchaseOrderLineVM> BadPOItems = [];

    List<PurchaseOrderLineVM> FilteredGoodPOItems = [];
    List<PurchaseOrderLineVM> FilteredBadPOItems = [];

    PurchaseOrderLineVM? LastScanned;
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    List<PurchaseOrderLineVM> POItems = [];

    PurchaseOrderLineVM? GoodSelectedLine;
    PurchaseOrderLineVM? BadSelectedLine;
    PurchaseOrderLineVM? MissingSelectedLine;

    int ScanCount { get; set; }
    int ActiveTabIndex { get; set; } = 0;

    bool SaveBtnDisabled => ScanCount == 0;

    bool NextScanIsBad = false;
    bool MoveOn = false;
    bool IsMissing { get; set; }
    bool IsWeightDialogOpen = false;

    ReceiveMode ReceiveByWeightMode = ReceiveMode.WithoutWeight;

    decimal? ChangeWeight = null;

    int UserId = 0;

    string Remarks = string.Empty;

    private string SearchText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        ActionGetPOItems = new AppAction<List<PurchaseOrderLineVM>>
        {
            Name = "GetPOItems",

            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<List<PurchaseOrderLineVM>>(
                    "/Receiving/PurchaseOrder/Items",
                    new { OrderNumber = OrderNumber });

                return res;
            },

            OnSuccess = async (result) =>
            {
                var source = result.Data ?? [];

                GoodPOItems = source.Select(line => new PurchaseOrderLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteSubsidiaryInternalId = line.NetsuiteSubsidiaryInternalId,
                    NetsuiteSubsidiaryDefaultBOInternalId = line.NetsuiteSubsidiaryDefaultBOInternalId,

                    NetsuiteLocationInternalId = line.NetsuiteLocationInternalId,
                    LocationName = line.LocationName,
                    LocationUsedBin = line.LocationUsedBin,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

                    NetsuiteVendorInternalId = line.NetsuiteVendorInternalId,
                    VendorName = line.VendorName,
                    VendorBinAssignmentId = line.VendorBinAssignmentId,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    MaterialWeight = line.MaterialWeight,
                    NetsuiteMaterialPrefferedBinId = line.NetsuiteMaterialPrefferedBinId,

                    LineQuantity = line.LineQuantity,
                    LineQuantityReceived = line.LineQuantityReceived,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,

                    IsBad = false,
                    IsMissing = false
                }).ToList();

                BadPOItems = source.Select(line => new PurchaseOrderLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteSubsidiaryInternalId = line.NetsuiteSubsidiaryInternalId,
                    NetsuiteSubsidiaryDefaultBOInternalId = line.NetsuiteSubsidiaryDefaultBOInternalId,

                    NetsuiteLocationInternalId = line.NetsuiteLocationInternalId,
                    LocationName = line.LocationName,
                    LocationUsedBin = line.LocationUsedBin,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

                    NetsuiteVendorInternalId = line.NetsuiteVendorInternalId,
                    VendorName = line.VendorName,
                    VendorBinAssignmentId = line.VendorBinAssignmentId,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    MaterialWeight = line.MaterialWeight,
                    NetsuiteMaterialPrefferedBinId = line.NetsuiteMaterialPrefferedBinId,

                    LineQuantity = line.LineQuantity,
                    LineQuantityReceived = line.LineQuantityReceived,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,

                    IsBad = true,
                    IsMissing = false
                }).ToList();

                ApplySearch();
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetItemBarcodes = new AppAction<List<ItemBarcodesPerUoMVM>>
        {
            Name = "GetItemBarcodes",

            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<List<ItemBarcodesPerUoMVM>>(
                    "/Item/Barcodes",
                    ItemRequest);

                return res;
            },

            OnSuccess = async (result) =>
            {
                ItemBarcodes = result.Data ?? [];

                await InvokeAsync(StateHasChanged);
            }
        };

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SavePurchaseOrderScan",

            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<bool>(
                    "/Receiving/PurchaseOrder/SaveScan",
                    new
                    {
                        PostPurchaseOrders = POItems,
                        UserId,
                        Remarks
                    });

                return res;
            },

            OnSuccess = async (result) =>
            {
                if (!result.Success)
                {
                    await Toast.Error(result.ErrorMessage);
                    return;
                }

                await Toast.Success("Scanned items saved successfully");

                NavManager.NavigateTo("/receiving");
            }
        };

        BroadcastService.BroadcastReceived += HandleItemScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ReceiveByWeightMode = await SelectWeightOption();

            await ActionFactory.ExecuteAppActionAsync(ActionGetPOItems);

            ItemRequest = GoodPOItems.Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);

            string? userAuth = await SecureStorage.GetAsync("UserAuth");

            if (userAuth is not null)
            {
                var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);

                if (auth is not null)
                {
                    UserId = auth.NetsuiteEmployeeInternalId;
                }
            }
        }

        if (GoodPOItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/IntersectionObserver.js");

            await JsObj.InvokeVoidAsync("ObserveRecentScanned");
        }
    }

    async Task LoadPurchaseOrder()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetPOItems);
    }

    #region Search
    private async Task OnSearchInput(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        ApplySearch();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearSearch()
    {
        SearchText = string.Empty;
        ApplySearch();
        await InvokeAsync(StateHasChanged);
    }

    private void ApplySearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredGoodPOItems = GoodPOItems.ToList();
            FilteredBadPOItems = BadPOItems.ToList();
            return;
        }

        var search = SearchText.Trim();

        FilteredGoodPOItems = GoodPOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();

        FilteredBadPOItems = BadPOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();
    }

    private static bool MatchesSearch(PurchaseOrderLineVM row, string search)
    {
        if (ContainsIgnoreCase(row.MaterialCode, search))
            return true;

        if (ContainsIgnoreCase(row.MaterialName, search))
            return true;

        if (ContainsIgnoreCase(row.LocationName, search))
            return true;

        if (ContainsIgnoreCase(row.UoMName, search))
            return true;

        return false;
    }

    private static bool ContainsIgnoreCase(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    private async void SelectGoodLine(PurchaseOrderLineVM item)
    {
        ValidManual = IsValidForManualEntry(item);

        if (ManualEntry)
        {
            if (!IsValidForManualEntry(item))
            {
                await Toast.Warning("Scan the same SKU 11 times before using Manual Entry.");
                return;
            }

            IsWeightDialogOpen = true;

            try
            {
                decimal badQty = BadPOItems.FirstOrDefault(y =>
                    y.LineSequenceNumber == item.LineSequenceNumber &&
                    y.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId)?.ScannedQuantity ?? 0;

                var result = await Dialog.OpenAsync<ManualEntryDialog>(
                    "Manual Entry",
                    new Dictionary<string, object>
                    {
                        { "ItemName", item.MaterialName },
                        { "PlannedQty", item.NSLineQuantityReceived },
                        { "GoodQty", item.ScannedQuantity},
                        { "PhysicalQty", item.PhysicalQuantity},
                        { "BadQty", badQty },
                    },
                    new DialogOptions
                    {
                        ShowClose = true
                    });

                if (result is ManualEntryDialog.ManualEntryResult entry)
                {
                    ScanCount = 1;

                    item.ScannedQuantity = entry.GoodQty;
                    item.ScannedWeight = item.MaterialWeight * entry.GoodQty;
                    item.PhysicalQuantity = entry.PhysicalQty;

                    var badItem = BadPOItems.FirstOrDefault(y =>
                        y.LineSequenceNumber == item.LineSequenceNumber &&
                        y.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

                    if (badItem != null)
                    {
                        badItem.ScannedQuantity = entry.BadQty;
                        badItem.ScannedWeight = badItem.MaterialWeight * entry.BadQty;
                    }

                    LastScanned = item;
                }
            }
            finally
            {
                IsWeightDialogOpen = false;
            }
        }

        if (GoodSelectedLine?.LineSequenceNumber == item.LineSequenceNumber)
        {
            GoodSelectedLine = null;
        }
        else
        {
            GoodSelectedLine = item;
        }

        await InvokeAsync(StateHasChanged);
    }

    private bool ValidManual { get; set; } = false;
    private bool IsValidForManualEntry(PurchaseOrderLineVM? row)
    {
        return row is not null
            && (row.NSLineQuantityPacked >= GlobalState.ManualEntryThreshold
                || row.NSLineQuantityReceived >= GlobalState.ManualEntryThreshold
                || row.ScannedQuantity >= GlobalState.ManualEntryThreshold)
            && row.ScanCount >= GlobalState.ManualEntryThreshold;
    }

    private bool IsSelectedGood(PurchaseOrderLineVM row)
    {
        return GoodSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    private void SelectBadLine(PurchaseOrderLineVM item)
    {
        if (BadSelectedLine?.LineSequenceNumber == item.LineSequenceNumber)
        {
            BadSelectedLine = null;
        }
        else
        {
            BadSelectedLine = item;
        }

        InvokeAsync(StateHasChanged);
    }

    private bool IsSelectedBad(PurchaseOrderLineVM row)
    {
        return BadSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    private void SelectMissingLine(PurchaseOrderLineVM item)
    {
        if (MissingSelectedLine?.LineSequenceNumber == item.LineSequenceNumber)
        {
            MissingSelectedLine = null;
        }
        else
        {
            MissingSelectedLine = item;
        }

        InvokeAsync(StateHasChanged);
    }

    private bool IsSelectedMissing(PurchaseOrderLineVM row)
    {
        return MissingSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            if (ScanState == ToggleState.Base &&
                !MoveOn &&
                !NegateQuantity)
            {
                return;
            }

            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
            {
                return;
            }

            if (MoveOn)
            {
                await MoveScan(scanned);
                return;
            }

            if (NegateQuantity)
            {
                await NegateScannedItem(scanned);
                return;
            }

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(
                    scanned,
                    StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            var goodLine = GoodPOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));

            var badLine = BadPOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var badlineTotal = badLine?.ScannedQuantity ?? 0;
            var goodLineTotal = goodLine.ScannedQuantity;

            var isOverScan =
                badlineTotal + goodLineTotal >= goodLine.NSLineQuantityReceived;

            if (isOverScan)
            {
                await Toast.Warning(
                    $"Over-scanning item: {goodLine.MaterialCode}.");

                return;
            }

            var scanQty = barcode.UoMRate / goodLine.UoMRate;

            var remainingQty =
                goodLine.NSLineQuantityReceived -
                (goodLine.ScannedQuantity +
                 (badLine?.ScannedQuantity ?? 0));

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning(
                    $"Scan quantity exceeds remaining quantity for item: {goodLine.MaterialCode}.");

                return;
            }

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (NextScanIsBad)
            {
                if (badLine is null)
                {
                    await Toast.Warning("Bad item line not found.");
                    return;
                }

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(
                        barcode.MaterialName,
                        barcode.UoMName);

                    if (!ChangeWeight.HasValue ||
                        ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning(
                            "Scan cancelled - no weight entered");

                        return;
                    }
                }
                else
                {
                    ChangeWeight = 0;
                }

                badLine.ScannedQuantity +=
                    barcode.UoMRate / badLine.UoMRate;

                badLine.ScannedWeight +=
                    ChangeWeight ?? 0m;

                badLine.ScanCount++;

                LastScanned = badLine;
            }
            else
            {
                decimal? weight = null;

                if (ChangeWeight.HasValue)
                {
                    weight = ChangeWeight;
                }
                else if (!barcode.DefaultWeight.HasValue)
                {
                    if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                    {
                        weight = await GetWeightAsync(
                            barcode.MaterialName,
                            barcode.UoMName);

                        if (!weight.HasValue ||
                            weight.Value == 0m)
                        {
                            await Toast.Warning(
                                "Scan cancelled - no weight entered");

                            return;
                        }
                    }
                    else
                    {
                        weight = 0;
                    }

                    barcode.DefaultWeight = weight;
                }
                else
                {
                    weight = barcode.DefaultWeight;
                }

                goodLine.ScannedQuantity +=
                    barcode.UoMRate / goodLine.UoMRate;

                goodLine.ScannedWeight +=
                    weight ?? 0m;

                goodLine.ScanCount++;

                LastScanned = goodLine;
            }

            ScanCount++;

            ChangeWeight = null;

            ValidManual = IsValidForManualEntry(LastScanned);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        POItems = GoodPOItems
            .Where(g =>
            {
                var badQty = BadPOItems.FirstOrDefault(b =>
                    b.LineSequenceNumber == g.LineSequenceNumber)?.ScannedQuantity ?? 0;

                return g.ScannedQuantity > 0 &&
                       g.ScannedQuantity + badQty <= g.NSLineQuantityReceived;
            })
            .Concat(BadPOItems.Where(x => x.ScannedQuantity > 0))
            .Select(x => new PurchaseOrderLineVM
            {
                NetsuiteOrderInternalId = x.NetsuiteOrderInternalId,
                OrderNumber = x.OrderNumber,
                OrderType = x.OrderType,
                OrderStatus = x.OrderStatus,

                NetsuiteSubsidiaryInternalId = x.NetsuiteSubsidiaryInternalId,
                NetsuiteSubsidiaryDefaultBOInternalId = x.NetsuiteSubsidiaryDefaultBOInternalId,

                NetsuiteLocationInternalId = x.NetsuiteLocationInternalId,
                LocationName = x.LocationName,
                LocationUsedBin = x.LocationUsedBin,

                LineSequenceNumber = x.LineSequenceNumber,
                TransactionLineType = x.TransactionLineType,

                NetsuiteVendorInternalId = x.NetsuiteVendorInternalId,
                VendorName = x.VendorName,
                VendorBinAssignmentId = x.VendorBinAssignmentId,

                NetsuiteMaterialInternalId = x.NetsuiteMaterialInternalId,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                MaterialWeight = x.MaterialWeight,
                NetsuiteMaterialPrefferedBinId = x.NetsuiteMaterialPrefferedBinId,

                LineQuantity = x.LineQuantity,
                LineQuantityReceived = x.LineQuantityReceived,

                NetsuiteUoMInternalId = x.NetsuiteUoMInternalId,
                UoMName = x.UoMName,
                UoMRate = x.UoMRate,

                ScanCount = x.ScanCount,
                IsBad = x.IsBad,
                ScannedQuantity = RoundOfNearestHundredThousands(x.ScannedQuantity),
                ScannedWeight = x.ScannedWeight
            })
            .ToList();

        if (POItems.Count == 0)
        {
            await Toast.Warning("There are no scanned items to save.");
            return;
        }

        try
        {
            var result = await Dialog.OpenAsync<RemarksDialog>(
                "Add Remarks",
                new Dictionary<string, object> { },
                new DialogOptions
                {
                    ShowClose = true
                });

            if (result is RemarksDialog.RemarksResult remarksResult)
            {
                if (string.IsNullOrWhiteSpace(remarksResult.Remarks))
                {
                   await Toast.Warning($"Please add remarks.");
                   return;
                }

                Remarks = remarksResult.Remarks;

                await ActionFactory.ExecuteAppActionAsync(
                    ActionSaveScan,
                    confirm: true,
                    showToast: true);
            }
        }
        catch (Exception e)
        {
            await Toast.Error($"Error: {e.Message}");
        }

        await InvokeAsync(StateHasChanged);
    }

    private bool IsActionPanelCollapsed;

    private void ToggleActionPanel()
    {
        IsActionPanelCollapsed = !IsActionPanelCollapsed;
    }

    void ToggleMove()
    {
        MoveOn = !MoveOn;

        NegateQuantity = false;
        ManualEntry = false;

        InvokeAsync(StateHasChanged);
    }

    private bool NegateQuantity;

    private void ToggleNegateQuantity()
    {
        NegateQuantity = !NegateQuantity;

        MoveOn = false;
        ManualEntry = false;

        InvokeAsync(StateHasChanged);
    }

    private bool ManualEntry = false;

    private void ToggleManualEntry()
    {
        ManualEntry = !ManualEntry;

        MoveOn = false;
        NegateQuantity = false;

        InvokeAsync(StateHasChanged);
    }

    async void ToggleWeight()
    {
        ChangeWeight = await GetWeightAsync("", "");
    }

    async Task MoveScan(string scanned)
    {
        try
        {
            PurchaseOrderLineVM? badLine;
            PurchaseOrderLineVM? goodLine;

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(
                    scanned,
                    StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            if (ActiveTabIndex == 1)
            {
                goodLine = GoodPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber ==
                        BadSelectedLine.LineSequenceNumber));

                badLine = BadPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber ==
                        BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber ==
                        GoodSelectedLine.LineSequenceNumber));

                badLine = BadPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber ==
                        GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null || badLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning(
                        "No scanned quantity to move for this item.");

                    return;
                }

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(
                        barcode.MaterialName,
                        barcode.UoMName);

                    if (!ChangeWeight.HasValue ||
                        ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning(
                            "Scan cancelled - no weight entered");

                        return;
                    }
                }
                else
                {
                    ChangeWeight = 0;
                }

                var badScannedQuantity =
                    barcode.UoMRate / badLine.UoMRate;

                var badScannedWeight =
                    ChangeWeight ?? 0m;

                if (badLine.ScannedQuantity <
                    badScannedQuantity)
                {
                    await Toast.Warning(
                        "Not enough scanned quantity to move.");

                    return;
                }

                badLine.ScannedQuantity -=
                    badScannedQuantity;

                badLine.ScannedWeight -=
                    badScannedWeight;

                goodLine.ScannedQuantity +=
                    badScannedQuantity;

                goodLine.ScannedWeight +=
                    badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning(
                        "No scanned quantity to move for this item.");

                    return;
                }

                decimal? weight = null;

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    weight = await GetWeightAsync(
                        barcode.MaterialName,
                        barcode.UoMName);

                    if (!weight.HasValue ||
                        weight.Value == 0m)
                    {
                        await Toast.Warning(
                            "Scan cancelled - no weight entered");

                        return;
                    }
                }
                else
                {
                    weight = 0;
                }

                var goodScannedQuantity =
                    barcode.UoMRate / goodLine.UoMRate;

                var goodScannedWeight =
                    weight ?? 0m;

                if (goodLine.ScannedQuantity <
                    goodScannedQuantity)
                {
                    await Toast.Warning(
                        "Not enough scanned quantity to move.");

                    return;
                }

                goodLine.ScannedQuantity -=
                    goodScannedQuantity;

                goodLine.ScannedWeight -=
                    goodScannedWeight;

                badLine.ScannedQuantity +=
                    goodScannedQuantity;

                badLine.ScannedWeight +=
                    goodScannedWeight;

                goodLine.ScanCount++;
            }

            ChangeWeight = null;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task NegateScannedItem(string scanned)
    {
        try
        {
            PurchaseOrderLineVM? badLine;
            PurchaseOrderLineVM? goodLine;

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(
                    scanned,
                    StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            if (ActiveTabIndex == 1)
            {
                goodLine = GoodPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber ==
                        BadSelectedLine.LineSequenceNumber));

                badLine = BadPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber ==
                        BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber ==
                        GoodSelectedLine.LineSequenceNumber));

                badLine = BadPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId ==
                        barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber ==
                        GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null || badLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning(
                        "No scanned quantity to remove for this item.");

                    return;
                }

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(
                        barcode.MaterialName,
                        barcode.UoMName);

                    if (!ChangeWeight.HasValue ||
                        ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning(
                            "Scan cancelled - no weight entered");

                        return;
                    }
                }
                else
                {
                    ChangeWeight = 0;
                }

                var badScannedQuantity =
                    barcode.UoMRate / badLine.UoMRate;

                var badScannedWeight =
                    ChangeWeight ?? 0m;

                if (badLine.ScannedQuantity <
                    badScannedQuantity)
                {
                    await Toast.Warning(
                        "Not enough scanned quantity to remove.");

                    return;
                }

                badLine.ScannedQuantity -=
                    badScannedQuantity;

                badLine.ScannedWeight -=
                    badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning(
                        "No scanned quantity to remove for this item.");

                    return;
                }

                decimal? weight = null;

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    weight = await GetWeightAsync(
                        barcode.MaterialName,
                        barcode.UoMName);

                    if (!weight.HasValue ||
                        weight.Value == 0m)
                    {
                        await Toast.Warning(
                            "Scan cancelled - no weight entered");

                        return;
                    }
                }
                else
                {
                    weight = 0;
                }

                var goodScannedQuantity =
                    barcode.UoMRate / goodLine.UoMRate;

                var goodScannedWeight =
                    weight ?? 0m;

                if (goodLine.ScannedQuantity <
                    goodScannedQuantity)
                {
                    await Toast.Warning(
                        "Not enough scanned quantity to remove.");

                    return;
                }

                goodLine.ScannedQuantity -=
                    goodScannedQuantity;

                goodLine.ScannedWeight -=
                    goodScannedWeight;

                goodLine.ScanCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    private async Task<decimal?> GetWeightAsync(
        string itemName,
        string uomName)
    {
        IsWeightDialogOpen = true;

        try
        {
            return await Dialog.OpenAsync<WeightInputDialog>(
                "Weight Input",
                new Dictionary<string, object>
                {
                    { "ItemName", itemName },
                    { "UomName", uomName }
                },
                new DialogOptions());
        }
        finally
        {
            IsWeightDialogOpen = false;
        }
    }

    private async Task<ReceiveMode> SelectWeightOption()
    {
        IsWeightDialogOpen = true;

        try
        {
            return await Dialog.OpenAsync<WeightOptionDialog>(
                "Weight Option",
                null,
                new DialogOptions
                {
                    ShowTitle = false,
                    ShowClose = false,
                    CloseDialogOnOverlayClick = false,
                    Resizable = false,
                    Draggable = false
                });
        }
        finally
        {
            IsWeightDialogOpen = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        BroadcastService.BroadcastReceived -= HandleItemScan;

        if (JsObj is not null)
        {
            try
            {
                JsObj.InvokeVoidAsync("UnObserveRecentScanned");
                await JsObj.InvokeVoidAsync("Dispose");
            }
            catch
            {
                // Ignore cleanup errors.
            }

            try
            {
                await JsObj.DisposeAsync();
            }
            finally
            {
                JsObj = null;
            }
        }
    }

    private async Task Refresh()
    {
        // Snapshot current scanned state before refreshing
        var scannedState = GoodPOItems
            .Concat(BadPOItems)
            .GroupBy(x => new
            {
                x.LineSequenceNumber,
                x.NetsuiteMaterialInternalId,
                x.IsBad
            })
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ScannedQuantity = g.First().ScannedQuantity,
                    ScannedWeight = g.First().ScannedWeight,
                    ScanCount = g.First().ScanCount,
                    PhysicalQuantity = g.First().PhysicalQuantity
                });

        // Reload PO items
        await ActionFactory.ExecuteAppActionAsync(ActionGetPOItems);

        // Restore scanned state
        foreach (var item in GoodPOItems.Concat(BadPOItems))
        {
            var key = new
            {
                item.LineSequenceNumber,
                item.NetsuiteMaterialInternalId,
                item.IsBad
            };

            if (scannedState.TryGetValue(key, out var scanned))
            {
                item.ScannedQuantity = scanned.ScannedQuantity;
                item.ScannedWeight = scanned.ScannedWeight;
                item.ScanCount = scanned.ScanCount;
                item.PhysicalQuantity = scanned.PhysicalQuantity;
            }
        }

        // Refresh barcode requests from the refreshed PO
        ItemRequest = GoodPOItems
            .Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId
            })
            .ToList();

        await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);

        await InvokeAsync(StateHasChanged);
    }


    #region Button States

    private ToggleState ScanState { get; set; } = ToggleState.Base;

    private string ScanStateIcon => ScanState switch
    {
        ToggleState.Base => "check",
        ToggleState.Good => "check",
        ToggleState.Bad => "block",
        _ => "check"
    };

    private string ScanStateLabel => ScanState switch
    {
        ToggleState.Base => "Good",
        ToggleState.Good => "Good",
        ToggleState.Bad => "Bad",
        _ => "Good"
    };

    private ButtonStyle ScanStateButtonStyle => ScanState switch
    {
        ToggleState.Base => ButtonStyle.Base,
        ToggleState.Good => ButtonStyle.Success,
        ToggleState.Bad => ButtonStyle.Danger,
        _ => ButtonStyle.Base
    };

    private void ToggleScanState()
    {
        ScanState = ScanState switch
        {
            ToggleState.Base => ToggleState.Good,
            ToggleState.Good => ToggleState.Bad,
            ToggleState.Bad => ToggleState.Good,
            _ => ToggleState.Good
        };

        NextScanIsBad = ScanState == ToggleState.Bad;

        InvokeAsync(StateHasChanged);
    }

    #endregion
}
