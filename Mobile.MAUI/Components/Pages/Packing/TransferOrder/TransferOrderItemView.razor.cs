using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.TransferOrder;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.Helpers.FormatHelper;

namespace Mobile.MAUI.Components.Pages.Packing.TransferOrder;

public partial class TransferOrderItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }
    AppAction<List<TransferOrderLineVM>> ActionGetTOItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<TransferOrderLineVM> GoodTOItems = [];
    List<TransferOrderLineVM> BadTOItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    List<TransferOrderLineVM> TOItems = [];

    TransferOrderLineVM? GoodSelectedLine;
    TransferOrderLineVM? BadSelectedLine;

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    int ActiveTabIndex { get; set; } = 0;

    bool NextScanIsBad = false;
    bool MoveOn = false;
    bool IsWeightDialogOpen = false;
    decimal? DefaultWeight = null;
    decimal? ChangeWeight = null;

    protected override async Task OnInitializedAsync()
    {
        ActionGetTOItems = new AppAction<List<TransferOrderLineVM>>
        {
            Name = "GetTOItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<TransferOrderLineVM>>("/Packing/TransferOrder/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                GoodTOItems = result.Data.Select(line => new TransferOrderLineVM
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
                    NetsuiteMaterialVendorAssignedBin = line.NetsuiteMaterialVendorAssignedBin,

                    LineQuantity = line.LineQuantity,
                    LineQuantityPacked = line.LineQuantityPacked,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,
                    IsBad = false,
                }).ToList() ?? [];

                BadTOItems = result.Data.Select(line => new TransferOrderLineVM
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
                    NetsuiteMaterialVendorAssignedBin = line.NetsuiteMaterialVendorAssignedBin,

                    LineQuantity = line.LineQuantity,
                    LineQuantityPacked = line.LineQuantityPacked,

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
                var res = await Client.Post<bool>("/Packing/TransferOrder/SaveScan", TOItems);
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
                NavManager.NavigateTo("/packing");
            }
        };

        BroadcastService.BroadcastReceived += HandleItemScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);

            ItemRequest = GoodTOItems.Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
        }

        if (TOItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadTransferOrder()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);
    }

    private void SelectGoodLine(TransferOrderLineVM item)
    {
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

    private bool IsSelectedGood(TransferOrderLineVM row)
    {
        return GoodSelectedLine?.LineSequenceNumber
            == row.LineSequenceNumber;
    }

    private void SelectBadLine(TransferOrderLineVM item)
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

    private bool IsSelectedBad(TransferOrderLineVM row)
    {
        return BadSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity) return;

            TransferOrderLineVM? badLine;

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

            var goodLine = GoodTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


            if (NextScanIsBad)
            {
                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }
            else
            {
                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            var isOverScan = badlineTotal + goodLineTotal >= goodLine.NSLineQuantityPacked;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {goodLine.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / goodLine.UoMRate;

            var remainingQty = goodLine.NSLineQuantityPacked - (goodLine.ScannedQuantity + (badLine?.ScannedQuantity ?? 0));

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {goodLine.MaterialCode}.");
                return;
            }

            //if (IsWeightDialogOpen)
            //{
            //    return;
            //}

            if (NextScanIsBad)
            {
                //ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                //{
                //    await Toast.Warning("Scan cancelled - no weight entered");
                //    return;
                //}

                badLine.ScannedQuantity += barcode.UoMRate / badLine.UoMRate;
                //badLine.ScannedWeight += ChangeWeight ?? 0m;
                badLine.ScanCount++;
            }
            else
            {
                //decimal? weight = null;

                //if (ChangeWeight.HasValue)
                //{
                //    weight = ChangeWeight;
                //}
                //else if (!barcode.DefaultWeight.HasValue)
                //{
                //    weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //    if (!weight.HasValue || weight.Value == 0m)
                //    {
                //        await Toast.Warning("Scan cancelled - no weight entered");
                //        return;
                //    }

                //    barcode.DefaultWeight = weight;
                //}
                //else
                //{
                //    weight = barcode.DefaultWeight;
                //}

                goodLine.ScannedQuantity += barcode.UoMRate / goodLine.UoMRate;
                //goodLine.ScannedWeight += weight ?? 0m;
                goodLine.ScanCount++;
            }

            ScanCount++;
            //ChangeWeight = null; // reset the ChangeWeight after each scan

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        TOItems = GoodTOItems.Where(x => x.NSLineQuantityPacked != 0)
            .Concat(BadTOItems.Where(x => x.NSLineQuantityPacked != 0))
            .Select(x => new TransferOrderLineVM
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

                NetsuiteMaterialPrefferedBinId = x.NetsuiteMaterialPrefferedBinId,
                NetsuiteMaterialVendorAssignedBin = x.NetsuiteMaterialVendorAssignedBin,

                LineQuantity = x.LineQuantity,
                LineQuantityPacked = x.LineQuantityPacked,

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

    //async void ToggleWeight()
    //{
    //    ChangeWeight = await GetWeightAsync("", "");
    //}

    private bool NegateQuantity;
    private void ToggleNegateQuantity()
    {
        NegateQuantity = !NegateQuantity;
        MoveOn = false;
    }

    async Task MoveScan(string scanned)
    {
        try
        {
            TransferOrderLineVM? badLine;
            TransferOrderLineVM? goodLine;

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
                goodLine = GoodTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadTOItems.FirstOrDefault(x =>
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

            //if (IsWeightDialogOpen)
            //{
            //    return;
            //}

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                //ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                //{
                //    await Toast.Warning("Scan cancelled - no weight entered");
                //    return;
                //}

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;
                //var badScannedWeight = barcode.UoMRate * (ChangeWeight ?? 0m);

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;
                //badLine.ScannedWeight -= badScannedWeight;

                goodLine.ScannedQuantity += badScannedQuantity;
                //goodLine.ScannedWeight += badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                //decimal? weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //if (!weight.HasValue || weight.Value == 0m)
                //{
                //    await Toast.Warning("Scan cancelled - no weight entered");
                //    return;
                //}

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                //var goodScannedWeight = barcode.UoMRate * (weight ?? 0m);

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;
                //goodLine.ScannedWeight -= goodScannedWeight;

                badLine.ScannedQuantity += goodScannedQuantity;
                //badLine.ScannedWeight += goodScannedWeight;

                goodLine.ScanCount++;
            }

            ScanCount++;
            //ChangeWeight = null; // reset the ChangeWeight after each scan

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
            TransferOrderLineVM? badLine;
            TransferOrderLineVM? goodLine;

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
                goodLine = GoodTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadTOItems.FirstOrDefault(x =>
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

            //if (IsWeightDialogOpen)
            //{
            //    return;
            //}

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                //ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                //{
                //    await Toast.Warning("Scan cancelled - no weight entered");
                //    return;
                //}

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;
                //var badScannedWeight = ChangeWeight ?? 0m;

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;
                //badLine.ScannedWeight -= badScannedWeight;

                badLine.ScanCount++;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                //decimal? weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                //if (!weight.HasValue || weight.Value == 0m)
                //{
                //    await Toast.Warning("Scan cancelled - no weight entered");
                //    return;
                //}

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                //var goodScannedWeight = weight ?? 0m;

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;
                //goodLine.ScannedWeight -= goodScannedWeight;

                goodLine.ScanCount++;
            }

            //ChangeWeight = null; // reset the ChangeWeight after each scan

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    //private async Task<decimal?> GetWeightAsync(string itemName, string uomName)
    //{
    //    IsWeightDialogOpen = true;

    //    try
    //    {
    //        return await Dialog.OpenAsync<WeightInputDialog>(
    //            "Weight Input",
    //            new Dictionary<string, object>
    //            {
    //                { "ItemName", itemName },
    //                { "UomName", uomName }
    //            },
    //            new DialogOptions());
    //    }
    //    finally
    //    {
    //        IsWeightDialogOpen = false;
    //    }
    //}

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