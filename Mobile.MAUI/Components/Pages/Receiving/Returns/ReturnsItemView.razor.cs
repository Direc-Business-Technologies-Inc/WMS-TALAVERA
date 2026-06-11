using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.Returns;

public partial class ReturnsItemView
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    AppAction<List<ReturnsLineVM>> ActionGetReturnsItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction ActionSaveScan { get; set; }

    List<ReturnsLineVM> ReturnsItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    //ReturnsLineVM? LastScanned => ReturnsItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    bool IsWeightDialogOpen = false;
    decimal? ChangeWeight = null;

    protected override async Task OnInitializedAsync()
    {
        ActionGetReturnsItems = new AppAction<List<ReturnsLineVM>>
        {
            Name = "GetReturnsItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<ReturnsLineVM>>("/Returns/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                ReturnsItems = result.Data.Select(line => new ReturnsLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,
                    TransferCategory = line.TransferCategory,

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
            Name = "SaveReturnsScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post("/Returns/SaveScan", ReturnsItems);
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
            await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);

            ItemRequest = ReturnsItems.Select(i => new BarcodeRequestVM
            {
                MaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
        }

        if (ReturnsItems.Count > 0 && JsObj is null)
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

            var line = ReturnsItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.MaterialInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var lineTotal = line.ScannedQuantity;

            var isOverScan = lineTotal >= line.NSLineQuantity;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {line.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / line.UoMRate;

            var remainingQty = line.NSLineQuantity - line.ScannedQuantity;

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {line.MaterialCode}.");
                return;
            }

            decimal? weight = null;

            if (ChangeWeight.HasValue)
            {
                weight = ChangeWeight;
            }
            else if (!line.DefaultWeight.HasValue)
            {
                if (IsWeightDialogOpen)
                {
                    return;
                }

                IsWeightDialogOpen = true;

                weight = await Dialog.OpenAsync<WeightInputDialog>(
                    "Weight Input",
                    new Dictionary<string, object>
                    {
                            { "ItemName", barcode.MaterialName }
                    },
                    new DialogOptions()
                );

                IsWeightDialogOpen = false;

                if (!weight.HasValue || weight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

                line.DefaultWeight = weight;
            }
            else
            {
                weight = line.DefaultWeight;
            }

            line.ScannedQuantity += barcode.UoMRate / line.UoMRate;
            line.ScannedWeight += barcode.UoMRate * (weight ?? 0m);
            line.ScanCount++;

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
        ReturnsItems = ReturnsItems.Where(x => x.ScannedQuantity != 0)
            .Select(x => new ReturnsLineVM
            {
                NetsuiteOrderInternalId = x.NetsuiteOrderInternalId,
                OrderNumber = x.OrderNumber,
                OrderType = x.OrderType,
                OrderStatus = x.OrderStatus,
                TransferCategory = x.TransferCategory,

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