using Mapster;
using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderItemView
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    AppAction<List<PurchaseOrderLineVM>> ActionGetPOItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction ActionSaveScan { get; set; }

    List<PurchaseOrderLineVM> GoodPOItems = [];
    List<PurchaseOrderLineVM> BadPOItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    //PurchaseOrderLineVM? LastScanned => POItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    bool NextScanIsBad = false;
    protected override async Task OnInitializedAsync()
    {
        ActionGetPOItems = new AppAction<List<PurchaseOrderLineVM>>
        {
            Name = "GetPOItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<PurchaseOrderLineVM>>("/Receiving/PO/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                GoodPOItems = result.Data ?? [];

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
            Name = "SavePurchaseOrderScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post("/Receiving/PO/SaveScan/Good", GoodPOItems);
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
            await ActionFactory.ExecuteAppActionAsync(ActionGetPOItems);

            ItemRequest = GoodPOItems.Select(i => new BarcodeRequestVM
            {
                MaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
        }

        if (GoodPOItems.Count > 0 && JsObj is null)
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

            if (NextScanIsBad)
            {
                var badLine = BadPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

                if (badLine is null)
                {
                    var sourceLine = GoodPOItems.FirstOrDefault(x =>
                        x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

                    if (sourceLine is null)
                    {
                        await Toast.Warning("Item not found in this PO.");
                        return;
                    }

                    badLine = new PurchaseOrderLineVM
                    {
                        NetsuiteOrderInternalId = sourceLine.NetsuiteOrderInternalId,
                        OrderNumber = sourceLine.OrderNumber,
                        OrderType = sourceLine.OrderType,
                        OrderStatus = sourceLine.OrderStatus,
                        NetsuiteLocationInternalId = sourceLine.NetsuiteLocationInternalId,
                        LocationName = sourceLine.LocationName,
                        LocationUsedBin = sourceLine.LocationUsedBin,
                        LineSequenceNumber = sourceLine.LineSequenceNumber,
                        TransactionLineType = sourceLine.TransactionLineType,
                        NetsuiteVendorInternalId = sourceLine.NetsuiteVendorInternalId,
                        VendorName = sourceLine.VendorName,
                        VendorBinAssignmentId = sourceLine.VendorBinAssignmentId,
                        NetsuiteMaterialInternalId = sourceLine.NetsuiteMaterialInternalId,
                        MaterialCode = sourceLine.MaterialCode,
                        MaterialName = sourceLine.MaterialName,
                        LineQuantity = sourceLine.LineQuantity,
                        NetsuiteUoMInternalId = sourceLine.NetsuiteUoMInternalId,
                        UoMName = sourceLine.UoMName,
                        UoMRate = sourceLine.UoMRate,

                        // scanning fields: start fresh for badLine
                        ScanCount = 0,
                        ScannedQuantity = 0,
                        IsBad = true
                    };

                    BadPOItems.Add(badLine);
                }

                badLine.ScannedQuantity += barcode.UoMRate;
                badLine.ScanCount++;
            }
            else
            {
                var goodLine = GoodPOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

                if (goodLine is null)
                {
                    await Toast.Warning("Item not found in this PO.");
                    return;
                }

                var isOverScan = goodLine.ScannedQuantity >= goodLine.LineQuantity;

                if(isOverScan)
                {
                    await Toast.Warning($"Over-scanning item: {goodLine.MaterialCode}.");
                    return;
                }

                goodLine.ScannedQuantity += barcode.UoMRate;
                goodLine.ScanCount++;
            }

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
        await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);

        StateHasChanged();
    }

    void ToggleQuality()
    {
        NextScanIsBad = !NextScanIsBad;
        InvokeAsync(StateHasChanged);
    }

    void ToggleQuality(PurchaseOrderLineVM row)
    {
        if (row is null) return;
        row.IsBad = !row.IsBad;
        InvokeAsync(StateHasChanged);
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