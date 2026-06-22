using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.TransferOrder;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.TransferOrder;

public partial class TransferOrderItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }
    AppAction<List<TransferOrderLineVM>> ActionGetTOItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction ActionSaveScan { get; set; }

    List<TransferOrderLineVM> GoodTOItems = [];
    List<TransferOrderLineVM> BadTOItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    List<TransferOrderLineVM> TOItems = [];

    TransferOrderLineVM? GoodSelectedLine;
    TransferOrderLineVM? BadSelectedLine;
    //TransferOrderLineVM? LastScanned => TOItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

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
                var res = await Client.Post<List<TransferOrderLineVM>>("/Receiving/TransferOrder/Items", new { OrderNumber = OrderNumber });
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

        ActionSaveScan = new AppAction
        {
            Name = "SaveTransferOrderScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post("/Receiving/TransferOrder/SaveScan", TOItems);
                return res;
            },
            OnSuccess = async (result) =>
            {
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
            await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);

            ItemRequest = GoodTOItems.Select(i => new BarcodeRequestVM
            {
                MaterialInternalId = i.NetsuiteMaterialInternalId,
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
            TransferOrderLineVM? badLine;

            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

            if (MoveOn)
            {
                await MoveScan(scanned);
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
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


            if (NextScanIsBad)
            {
                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }
            else
            {
                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
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
                ChangeWeight = await GetWeightAsync(barcode.MaterialName);

                if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

                badLine.ScannedQuantity += barcode.UoMRate / badLine.UoMRate;
                badLine.ScannedWeight += barcode.UoMRate * (ChangeWeight ?? 0m);
                badLine.ScanCount++;
            }
            else
            {
                decimal? weight = null;

                if (ChangeWeight.HasValue)
                {
                    weight = ChangeWeight;
                }
                else if (!goodLine.DefaultWeight.HasValue)
                {
                    weight = await GetWeightAsync(barcode.MaterialName);

                    if (!weight.HasValue || weight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }

                    goodLine.DefaultWeight = weight;
                }
                else
                {
                    weight = goodLine.DefaultWeight;
                }

                goodLine.ScannedQuantity += barcode.UoMRate / goodLine.UoMRate;
                goodLine.ScannedWeight += barcode.UoMRate * (weight ?? 0m);
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
        TOItems = GoodTOItems.Where(x => x.NSLineQuantityReceived != 0)
            .Concat(BadTOItems.Where(x => x.NSLineQuantityReceived != 0))
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
                LineQuantity = x.LineQuantity,
                LineQuantityReceived = x.LineQuantityReceived,
                NetsuiteUoMInternalId = x.NetsuiteUoMInternalId,
                UoMName = x.UoMName,
                UoMRate = x.UoMRate,

                ScanCount = x.ScanCount,
                IsBad = x.IsBad,
                ScannedQuantity = x.ScannedQuantity,
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

    async void ToggleWeight()
    {
        ChangeWeight = await Dialog.OpenAsync<WeightInputDialog>(
                    "Weight Input",
                    new Dictionary<string, object>
                    {
                        { "ItemName", "" }
                    },
                    new DialogOptions()
                );
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
                x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
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

                ChangeWeight = await GetWeightAsync(barcode.MaterialName);

                if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;
                var badScannedWeight = barcode.UoMRate * (ChangeWeight ?? 0m);

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

                decimal? weight = await GetWeightAsync(barcode.MaterialName);

                if (!weight.HasValue || weight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                var goodScannedWeight = barcode.UoMRate * (weight ?? 0m);

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

    private async Task<decimal?> GetWeightAsync(string itemName)
    {
        IsWeightDialogOpen = true;

        try
        {
            return await Dialog.OpenAsync<WeightInputDialog>(
                "Weight Input",
                new Dictionary<string, object>
                {
                { "ItemName", itemName }
                },
                new DialogOptions());
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
}