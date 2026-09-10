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
    List<PurchaseOrderLineVM> MissingItems = [];
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

                MissingItems = source.Select(line => new PurchaseOrderLineVM
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
                    IsMissing = true
                }).ToList();

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
                        UserId
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

            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadPurchaseOrder()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetPOItems);
    }

    private async void SelectGoodLine(PurchaseOrderLineVM item)
    {
        if (ManualEntry)
        {
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

                    var badItem = BadPOItems.FirstOrDefault(y =>
                        y.LineSequenceNumber == item.LineSequenceNumber &&
                        y.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

                    if (badItem != null)
                    {
                        badItem.ScannedQuantity = entry.BadQty;
                    }

                    // Manual entry is Good/Bad only.
                    // Reset Missing for this manual-entry operation.
                    var missingItem = MissingItems.FirstOrDefault(y =>
                        y.LineSequenceNumber == item.LineSequenceNumber &&
                        y.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

                    if (missingItem != null)
                    {
                        missingItem.ScannedQuantity = entry.MissingQty;
                    }
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

            if (ScanState == ToggleState.Missing)
            {
                await ScanMissingItem(scanned);
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
            }

            ScanCount++;

            ChangeWeight = null;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    private async Task ScanMissingItem(string scanned)
    {
        try
        {
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

            var missingLine = MissingItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId ==
                    barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber ==
                    GoodSelectedLine.LineSequenceNumber));

            if (missingLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            if (IsWeightDialogOpen)
            {
                return;
            }

            var goodLine = GoodPOItems.FirstOrDefault(x =>
                x.LineSequenceNumber ==
                    missingLine.LineSequenceNumber &&
                x.NetsuiteMaterialInternalId ==
                    missingLine.NetsuiteMaterialInternalId);

            var badLine = BadPOItems.FirstOrDefault(x =>
                x.LineSequenceNumber ==
                    missingLine.LineSequenceNumber &&
                x.NetsuiteMaterialInternalId ==
                    missingLine.NetsuiteMaterialInternalId);

            var goodQty = goodLine?.ScannedQuantity ?? 0;
            var badQty = badLine?.ScannedQuantity ?? 0;
            var missingQty = missingLine.ScannedQuantity;

            var totalQty = goodQty + badQty + missingQty;

            if (totalQty >= missingLine.NSLineQuantityReceived)
            {
                await Toast.Warning(
                    $"Over-scanning item: {missingLine.MaterialCode}.");

                return;
            }

            var scanQty =
                barcode.UoMRate / missingLine.UoMRate;

            var remainingQty =
                missingLine.NSLineQuantityReceived - totalQty;

            if (scanQty > remainingQty)
            {
                await Toast.Warning(
                    $"Missing quantity exceeds remaining quantity for item: {missingLine.MaterialCode}.");

                return;
            }

            // Missing does not represent physical received weight.
            // Therefore we only increment quantity.
            missingLine.ScannedQuantity += scanQty;
            missingLine.ScanCount++;

            ScanCount++;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        POItems = [];

        foreach (var good in GoodPOItems)
        {
            var bad = BadPOItems.FirstOrDefault(b =>
                b.LineSequenceNumber == good.LineSequenceNumber &&
                b.NetsuiteMaterialInternalId ==
                    good.NetsuiteMaterialInternalId);

            var missing = MissingItems.FirstOrDefault(m =>
                m.LineSequenceNumber == good.LineSequenceNumber &&
                m.NetsuiteMaterialInternalId ==
                    good.NetsuiteMaterialInternalId);

            var badQty = bad?.ScannedQuantity ?? 0;
            var missingQty = missing?.ScannedQuantity ?? 0;

            var totalQty =
                good.ScannedQuantity +
                badQty +
                missingQty;

            // Nothing scanned for this line.
            if (totalQty == 0)
            {
                continue;
            }

            // Prevent total classification from exceeding PO quantity.
            if (totalQty > good.NSLineQuantityReceived)
            {
                await Toast.Warning(
                    $"Scanned quantity exceeds PO quantity for item: {good.MaterialCode}");

                return;
            }

            // GOOD
            if (good.ScannedQuantity > 0)
            {
                POItems.Add(new PurchaseOrderLineVM
                {
                    NetsuiteOrderInternalId = good.NetsuiteOrderInternalId,
                    OrderNumber = good.OrderNumber,
                    OrderType = good.OrderType,
                    OrderStatus = good.OrderStatus,

                    NetsuiteSubsidiaryInternalId =
                        good.NetsuiteSubsidiaryInternalId,

                    NetsuiteSubsidiaryDefaultBOInternalId =
                        good.NetsuiteSubsidiaryDefaultBOInternalId,

                    NetsuiteLocationInternalId =
                        good.NetsuiteLocationInternalId,

                    LocationName = good.LocationName,
                    LocationUsedBin = good.LocationUsedBin,

                    LineSequenceNumber = good.LineSequenceNumber,
                    TransactionLineType = good.TransactionLineType,

                    NetsuiteVendorInternalId =
                        good.NetsuiteVendorInternalId,

                    VendorName = good.VendorName,
                    VendorBinAssignmentId = good.VendorBinAssignmentId,

                    NetsuiteMaterialInternalId =
                        good.NetsuiteMaterialInternalId,

                    MaterialCode = good.MaterialCode,
                    MaterialName = good.MaterialName,
                    MaterialWeight = good.MaterialWeight,

                    NetsuiteMaterialPrefferedBinId =
                        good.NetsuiteMaterialPrefferedBinId,

                    LineQuantity = good.LineQuantity,
                    LineQuantityReceived = good.LineQuantityReceived,

                    NetsuiteUoMInternalId =
                        good.NetsuiteUoMInternalId,

                    UoMName = good.UoMName,
                    UoMRate = good.UoMRate,

                    ScanCount = good.ScanCount,
                    IsBad = false,
                    IsMissing = false,

                    ScannedQuantity =
                        RoundOfNearestHundredThousands(
                            good.ScannedQuantity),

                    ScannedWeight =
                        good.ScannedWeight
                });
            }

            // BAD
            if (badQty > 0 && bad is not null)
            {
                POItems.Add(new PurchaseOrderLineVM
                {
                    NetsuiteOrderInternalId =
                        bad.NetsuiteOrderInternalId,

                    OrderNumber = bad.OrderNumber,
                    OrderType = bad.OrderType,
                    OrderStatus = bad.OrderStatus,

                    NetsuiteSubsidiaryInternalId =
                        bad.NetsuiteSubsidiaryInternalId,

                    NetsuiteSubsidiaryDefaultBOInternalId =
                        bad.NetsuiteSubsidiaryDefaultBOInternalId,

                    NetsuiteLocationInternalId =
                        bad.NetsuiteLocationInternalId,

                    LocationName = bad.LocationName,
                    LocationUsedBin = bad.LocationUsedBin,

                    LineSequenceNumber =
                        bad.LineSequenceNumber,

                    TransactionLineType =
                        bad.TransactionLineType,

                    NetsuiteVendorInternalId =
                        bad.NetsuiteVendorInternalId,

                    VendorName = bad.VendorName,
                    VendorBinAssignmentId =
                        bad.VendorBinAssignmentId,

                    NetsuiteMaterialInternalId =
                        bad.NetsuiteMaterialInternalId,

                    MaterialCode = bad.MaterialCode,
                    MaterialName = bad.MaterialName,
                    MaterialWeight = bad.MaterialWeight,

                    NetsuiteMaterialPrefferedBinId =
                        bad.NetsuiteMaterialPrefferedBinId,

                    LineQuantity = bad.LineQuantity,
                    LineQuantityReceived =
                        bad.LineQuantityReceived,

                    NetsuiteUoMInternalId =
                        bad.NetsuiteUoMInternalId,

                    UoMName = bad.UoMName,
                    UoMRate = bad.UoMRate,

                    ScanCount = bad.ScanCount,
                    IsBad = true,
                    IsMissing = false,

                    ScannedQuantity =
                        RoundOfNearestHundredThousands(
                            bad.ScannedQuantity),

                    ScannedWeight =
                        bad.ScannedWeight
                });
            }

            // MISSING
            if (missingQty > 0 && missing is not null)
            {
                POItems.Add(new PurchaseOrderLineVM
                {
                    NetsuiteOrderInternalId =
                        missing.NetsuiteOrderInternalId,

                    OrderNumber = missing.OrderNumber,
                    OrderType = missing.OrderType,
                    OrderStatus = missing.OrderStatus,

                    NetsuiteSubsidiaryInternalId =
                        missing.NetsuiteSubsidiaryInternalId,

                    NetsuiteSubsidiaryDefaultBOInternalId =
                        missing.NetsuiteSubsidiaryDefaultBOInternalId,

                    NetsuiteLocationInternalId =
                        missing.NetsuiteLocationInternalId,

                    LocationName = missing.LocationName,
                    LocationUsedBin = missing.LocationUsedBin,

                    LineSequenceNumber =
                        missing.LineSequenceNumber,

                    TransactionLineType =
                        missing.TransactionLineType,

                    NetsuiteVendorInternalId =
                        missing.NetsuiteVendorInternalId,

                    VendorName = missing.VendorName,
                    VendorBinAssignmentId =
                        missing.VendorBinAssignmentId,

                    NetsuiteMaterialInternalId =
                        missing.NetsuiteMaterialInternalId,

                    MaterialCode = missing.MaterialCode,
                    MaterialName = missing.MaterialName,
                    MaterialWeight = missing.MaterialWeight,

                    NetsuiteMaterialPrefferedBinId =
                        missing.NetsuiteMaterialPrefferedBinId,

                    LineQuantity = missing.LineQuantity,
                    LineQuantityReceived =
                        missing.LineQuantityReceived,

                    NetsuiteUoMInternalId =
                        missing.NetsuiteUoMInternalId,

                    UoMName = missing.UoMName,
                    UoMRate = missing.UoMRate,

                    ScanCount = missing.ScanCount,
                    IsBad = false,
                    IsMissing = true,

                    ScannedQuantity =
                        RoundOfNearestHundredThousands(
                            missing.ScannedQuantity),

                    ScannedWeight = 0
                });
            }
        }

        if (POItems.Count == 0)
        {
            await Toast.Warning("There are no scanned items to save.");
            return;
        }

        await ActionFactory.ExecuteAppActionAsync(
            ActionSaveScan,
            confirm: true,
            showToast: true);

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

    #region Button States

    private ToggleState ScanState { get; set; } = ToggleState.Base;

    private string ScanStateIcon => ScanState switch
    {
        ToggleState.Base => "check",
        ToggleState.Good => "check",
        ToggleState.Bad => "block",
        ToggleState.Missing => "remove_circle",
        _ => "check"
    };

    private string ScanStateLabel => ScanState switch
    {
        ToggleState.Base => "Good",
        ToggleState.Good => "Good",
        ToggleState.Bad => "Bad",
        ToggleState.Missing => "Missing",
        _ => "Good"
    };

    private ButtonStyle ScanStateButtonStyle => ScanState switch
    {
        ToggleState.Base => ButtonStyle.Base,
        ToggleState.Good => ButtonStyle.Success,
        ToggleState.Bad => ButtonStyle.Danger,
        ToggleState.Missing => ButtonStyle.Warning,
        _ => ButtonStyle.Base
    };

    private void ToggleScanState()
    {
        ScanState = ScanState switch
        {
            ToggleState.Base => ToggleState.Good,
            ToggleState.Good => ToggleState.Bad,
            ToggleState.Bad => ToggleState.Missing,
            ToggleState.Missing => ToggleState.Good,
            _ => ToggleState.Good
        };

        NextScanIsBad = ScanState == ToggleState.Bad;

        // Missing is a scanning state, but Move/Remove/Manual
        // should not remain active when switching to it.
        if (ScanState == ToggleState.Missing)
        {
            MoveOn = false;
            NegateQuantity = false;
            ManualEntry = false;
        }

        InvokeAsync(StateHasChanged);
    }

    #endregion
}
