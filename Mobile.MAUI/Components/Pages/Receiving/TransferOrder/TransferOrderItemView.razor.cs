using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.TransferOrder;

public partial class TransferOrderItemView
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


    //TransferOrderLineVM? LastScanned => TOItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    bool NextScanIsBad = false;
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
                var res = await Client.Post<List<TransferOrderLineVM>>("/Receiving/TO/Items", new { OrderNumber = OrderNumber });
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
                var res = await Client.Post("/Receiving/TO/SaveScan", TOItems);
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

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(scanned, StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            if (!NextScanIsBad)
            {
                ChangeWeight = await Dialog.OpenAsync<WeightInputDialog>(
                    "Weight Input",
                    new Dictionary<string, object>
                    {
                        { "ItemName", barcode.MaterialName }
                    },
                    new DialogOptions()
                );
            }
            else if (DefaultWeight is null)
            {
                DefaultWeight = await Dialog.OpenAsync<WeightInputDialog>(
                    "Weight Input",
                    new Dictionary<string, object>
                    {
                        { "ItemName", barcode.MaterialName }
                    },
                    new DialogOptions()
                );
            }
            

            if (!DefaultWeight.HasValue)
            {
                return;
            }

            if (DefaultWeight.Value == 0m)
            {
                await Toast.Warning("Scan cancelled - no weight entered");
                return;
            }

            var goodLine = GoodTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

            var badLine = BadTOItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            var isOverScan = badlineTotal + goodLineTotal >= goodLine.NSLineQuantity;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {goodLine.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / goodLine.UoMRate;

            var remainingQty = goodLine.NSLineQuantity - (goodLine.ScannedQuantity + (badLine?.ScannedQuantity ?? 0));

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {goodLine.MaterialCode}.");
                return;
            }

            var weight = ChangeWeight ?? DefaultWeight;

            if (NextScanIsBad)
            {
                badLine.ScannedQuantity += barcode.UoMRate / badLine.UoMRate;
                badLine.ScannedWeight += barcode.UoMRate * (weight ?? 0m);
                badLine.ScanCount++;
            }
            else
            {
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
        TOItems = GoodTOItems
            .Concat(BadTOItems)
            .Select(x => new TransferOrderLineVM
            {
                NetsuiteOrderInternalId = x.NetsuiteOrderInternalId,
                OrderNumber = x.OrderNumber,
                OrderType = x.OrderType,
                OrderStatus = x.OrderStatus,
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

    public async ValueTask DisposeAsync()
    {
        BroadcastService.BroadcastReceived -= HandleItemScan;
        if (JsObj is not null)
        {
            await JsObj.InvokeVoidAsync("UnObserve");
            await JsObj.DisposeAsync();
        }
    }
}