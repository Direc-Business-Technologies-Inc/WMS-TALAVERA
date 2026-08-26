using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.ItemFulfillment;
using System.Text.Json;
using static Mobile.MAUI.Components.Reusables.WeightOptionDialog;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.Helpers.FormatHelper;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.TransferOrder;

public partial class TOxItemFulfillmentItemView : IAsyncDisposable
{
    [Parameter]
    public int NetsuiteOrderInternalId { get; set; }

    [Parameter]
    public string TOOrderNumber { get; set; }

    [Parameter]
    public string OrderNumber { get; set; }

    string BackPath => $"/receiving/transferorder/itemfulfillment/{NetsuiteOrderInternalId}/{TOOrderNumber}";

    private IJSObjectReference JsObj { get; set; }
    AppAction<List<TOxItemFulfillmentLineVM>> ActionGetTOxItemfulfillmentItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<TOxItemFulfillmentLineVM> GoodIFItems = [];
    List<TOxItemFulfillmentLineVM> BadIFItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    List<TOxItemFulfillmentLineVM> IFItems = [];

    TOxItemFulfillmentLineVM? GoodSelectedLine;
    TOxItemFulfillmentLineVM? BadSelectedLine;
    //TOxItemFulfillmentLineVM? LastScanned => IFItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    int ActiveTabIndex { get; set; } = 0;

    bool NextScanIsBad = false;
    bool MoveOn = false;
    bool IsWeightDialogOpen = false;
    decimal? DefaultWeight = null;
    decimal? ChangeWeight = null;
    int UserId = 0;

    ReceiveMode ReceiveByWeightMode = ReceiveMode.WithoutWeight;

    protected override async Task OnInitializedAsync()
    {
        ActionGetTOxItemfulfillmentItems = new AppAction<List<TOxItemFulfillmentLineVM>>
        {
            Name = "GetTOxItemfulfillmentItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<TOxItemFulfillmentLineVM>>("/Receiving/TransferOrder/ItemFulfillment/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                GoodIFItems = result.Data.Select(line => new TOxItemFulfillmentLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteFromLocationInternalId = line.NetsuiteFromLocationInternalId,
                    NetsuiteToLocationInternalId = line.NetsuiteToLocationInternalId,

                    NetsuiteFromSubsidiaryInternalId = line.NetsuiteFromSubsidiaryInternalId,
                    NetsuiteSubsidiaryDefaultBOInternalId = line.NetsuiteSubsidiaryDefaultBOInternalId,
                    NetsuiteToSubsidiaryInternalId = line.NetsuiteToSubsidiaryInternalId,

                    LocationName = line.LocationName,
                    LocationUsedBin = line.LocationUsedBin,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

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
                }).ToList() ?? [];

                BadIFItems = result.Data.Select(line => new TOxItemFulfillmentLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteFromLocationInternalId = line.NetsuiteFromLocationInternalId,
                    NetsuiteToLocationInternalId = line.NetsuiteToLocationInternalId,

                    NetsuiteFromSubsidiaryInternalId = line.NetsuiteFromSubsidiaryInternalId,
                    NetsuiteSubsidiaryDefaultBOInternalId = line.NetsuiteSubsidiaryDefaultBOInternalId,
                    NetsuiteToSubsidiaryInternalId = line.NetsuiteToSubsidiaryInternalId,

                    LocationName = line.LocationName,
                    LocationUsedBin = line.LocationUsedBin,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    MaterialWeight = line.MaterialWeight,

                    LineQuantity = line.LineQuantity,
                    LineQuantityReceived = line.LineQuantityReceived,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,
                    IsBad = true,
                }).ToList() ?? [];

                await InvokeAsync(StateHasChanged);
            },
        };

        ActionGetItemBarcodes = new AppAction<List<ItemBarcodesPerUoMVM>>
        {
            Name = "GetItemBarcodes",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<ItemBarcodesPerUoMVM>>("/Item/Barcodes", ItemRequest);
                return res;
            },
            OnSuccess = async (result) =>
            {
                ItemBarcodes = result.Data ?? [];

                await InvokeAsync(StateHasChanged);
            },
        };

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveTransferOrderScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/Receiving/TransferOrder/SaveScan", new { PostTransferOrder = IFItems, TONetsuiteOrderInternalId = NetsuiteOrderInternalId, UserId });
                return res;
            },
            OnSuccess = async (result) =>
            {
                if (!result.Success)
                {
                    await Toast.Error(result.ErrorMessage);
                    return;
                }

                await Toast.Success("Scanned items saved sucessfully");
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

            await ActionFactory.ExecuteAppActionAsync(ActionGetTOxItemfulfillmentItems);

            ItemRequest = GoodIFItems.Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);

            string? userAuth = await SecureStorage.GetAsync("UserAuth");
            if (userAuth is not null)
            {
                var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);

                UserId = auth.NetsuiteEmployeeInternalId;
            }
        }

        if (IFItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadTransferOrder()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOxItemfulfillmentItems);
    }

    private async void SelectGoodLine(TOxItemFulfillmentLineVM item)
    {
        if (ManualEntry)
        {
            IsWeightDialogOpen = true;

            try
            {
                var result = await Dialog.OpenAsync<ManualEntryDialog>(
                    "Manual Entry",
                    new Dictionary<string, object>
                    {
                        { "ItemName", item.MaterialName },
                        { "PlannedQty", item.NSLineQuantityReceived }
                    },
                    new DialogOptions
                    {
                        ShowClose = true,
                    });

                if (result is ManualEntryDialog.ManualEntryResult entry)
                {
                    item.ScannedQuantity = entry.GoodQty;

                    if (entry.BadQty != 0)
                    {
                        var badItem = BadIFItems.FirstOrDefault(
                            y =>
                            y.LineSequenceNumber == item.LineSequenceNumber &&
                            y.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

                        if (badItem != null)
                        {
                            badItem.ScannedQuantity = entry.BadQty;
                        }
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

        InvokeAsync(StateHasChanged);
    }

    private bool IsSelectedGood(TOxItemFulfillmentLineVM row)
    {
        return GoodSelectedLine?.LineSequenceNumber
            == row.LineSequenceNumber;
    }

    private void SelectBadLine(TOxItemFulfillmentLineVM item)
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

    private bool IsSelectedBad(TOxItemFulfillmentLineVM row)
    {
        return BadSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity) return;

            TOxItemFulfillmentLineVM? badLine;

            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

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
                x.MaterialBarcode.Equals(scanned, StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            var goodLine = GoodIFItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));

            badLine = BadIFItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this TO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            var isOverScan = badlineTotal + goodLineTotal >= goodLine.NSLineQuantityReceived;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {goodLine.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / goodLine.UoMRate;

            var remainingQty = goodLine.NSLineQuantityReceived - (goodLine.ScannedQuantity + (badLine?.ScannedQuantity ?? 0));

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {goodLine.MaterialCode}.");
                return;
            }

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (NextScanIsBad)
            {

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }
                }
                else
                    ChangeWeight = 0;

                badLine.ScannedQuantity += barcode.UoMRate / badLine.UoMRate;
                badLine.ScannedWeight += ChangeWeight ?? 0m;
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
                        weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                        if (!weight.HasValue || weight.Value == 0m)
                        {
                            await Toast.Warning("Scan cancelled - no weight entered");
                            return;
                        }
                    }
                    else
                        weight = 0;

                    barcode.DefaultWeight = weight;
                }
                else
                {
                    weight = barcode.DefaultWeight;
                }

                goodLine.ScannedQuantity += barcode.UoMRate / goodLine.UoMRate;
                goodLine.ScannedWeight += weight ?? 0m;
                goodLine.ScanCount++;
            }

            ScanCount++;
            ChangeWeight = null; // reset the ChangeWeight after each scan

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        IFItems = GoodIFItems
            .Where(g =>
            {
                var bad = BadIFItems.FirstOrDefault(b =>
                    b.LineSequenceNumber == g.LineSequenceNumber);

                var badQty = bad?.ScannedQuantity ?? 0;

                return g.ScannedQuantity > 0 ||
                        (g.ScannedQuantity + badQty) <= g.NSLineQuantityReceived;
            })
            .Concat(BadIFItems.Where(x => x.NSLineQuantityReceived != 0))
            .Select(x => new TOxItemFulfillmentLineVM
            {
                NetsuiteOrderInternalId = x.NetsuiteOrderInternalId,
                OrderNumber = x.OrderNumber,
                OrderType = x.OrderType,
                OrderStatus = x.OrderStatus,

                NetsuiteFromLocationInternalId = x.NetsuiteFromLocationInternalId,
                NetsuiteToLocationInternalId = x.NetsuiteToLocationInternalId,

                NetsuiteFromSubsidiaryInternalId = x.NetsuiteFromSubsidiaryInternalId,
                NetsuiteSubsidiaryDefaultBOInternalId = x.NetsuiteSubsidiaryDefaultBOInternalId,
                NetsuiteToSubsidiaryInternalId = x.NetsuiteToSubsidiaryInternalId,

                LocationName = x.LocationName,
                LocationUsedBin = x.LocationUsedBin,

                LineSequenceNumber = x.LineSequenceNumber,
                TransactionLineType = x.TransactionLineType,

                NetsuiteMaterialInternalId = x.NetsuiteMaterialInternalId,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                MaterialWeight = x.MaterialWeight,
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

        await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);

        await InvokeAsync(StateHasChanged);
    }

    void ToggleQuality()
    {
        NextScanIsBad = !NextScanIsBad;
        InvokeAsync(StateHasChanged);
    }

    void ToggleMove()
    {
        MoveOn = !MoveOn;
        InvokeAsync(StateHasChanged);
    }

    private bool IsActionPanelCollapsed;
    private void ToggleActionPanel()
    {
        IsActionPanelCollapsed = !IsActionPanelCollapsed;
    }

    private bool NegateQuantity;
    private void ToggleNegateQuantity()
    {
        NegateQuantity = !NegateQuantity;
        MoveOn = false;
    }

    private bool ManualEntry = false;
    private void ToggleManualEntry()
    {
        ManualEntry = !ManualEntry;
        MoveOn = false;
        NegateQuantity = false;
    }

    async void ToggleWeight()
    {
        ChangeWeight = await GetWeightAsync("", "");
    }

    async Task MoveScan(string scanned)
    {
        try
        {
            TOxItemFulfillmentLineVM? badLine;
            TOxItemFulfillmentLineVM? goodLine;

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(scanned, StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            if (ActiveTabIndex == 1)
            {
                goodLine = GoodIFItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadIFItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodIFItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadIFItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this TO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }   
                }
                else
                    ChangeWeight = 0;

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;
                var badScannedWeight = ChangeWeight ?? 0m;

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;
                badLine.ScannedWeight -= badScannedWeight;

                goodLine.ScannedQuantity += badScannedQuantity;
                goodLine.ScannedWeight += badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                decimal? weight = null;

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!weight.HasValue || weight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }
                }
                else
                    weight = 0;

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                var goodScannedWeight = weight ?? 0m;

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;
                goodLine.ScannedWeight -= goodScannedWeight;

                badLine.ScannedQuantity += goodScannedQuantity;
                badLine.ScannedWeight += goodScannedWeight;

                goodLine.ScanCount++;
            }

            ScanCount++;
            ChangeWeight = null; // reset the ChangeWeight after each scan

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
            TOxItemFulfillmentLineVM? badLine;
            TOxItemFulfillmentLineVM? goodLine;

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(scanned, StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            if (ActiveTabIndex == 1)
            {
                goodLine = GoodIFItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadIFItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodIFItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadIFItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this TO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            if (IsWeightDialogOpen)
            {
                return;
            }

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }


                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }
                }
                else
                    ChangeWeight = 0;

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;
                var badScannedWeight = ChangeWeight ?? 0m;

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;
                badLine.ScannedWeight -= badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                decimal? weight = null;

                if (ReceiveByWeightMode == ReceiveMode.WithWeight)
                {
                    weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!weight.HasValue || weight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }
                }
                else
                    weight = 0;

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                var goodScannedWeight = weight ?? 0m;

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;
                goodLine.ScannedWeight -= goodScannedWeight;

                goodLine.ScanCount++;
            }

            ChangeWeight = null; // reset the ChangeWeight after each scan

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    private async Task<decimal?> GetWeightAsync(string itemName, string uomName)
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
                // ignore cleanup errors
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
            ToggleState.Bad => ToggleState.Base,
            _ => ToggleState.Base
        };

        NextScanIsBad = ScanState switch
        {
            ToggleState.Base => false,
            ToggleState.Good => false,
            ToggleState.Bad => true,
            _ => false
        };

        InvokeAsync(StateHasChanged);
    }
    #endregion
}