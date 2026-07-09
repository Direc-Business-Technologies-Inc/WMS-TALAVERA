using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Returns;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.Helpers.FormatHelper;

namespace Mobile.MAUI.Components.Pages.Packing.Returns;

public partial class ReturnsItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }
    private IJSObjectReference JsObj { get; set; }
    AppAction<List<ReturnsLineVM>> ActionGetReturnsItems { get; set; }
    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<ReturnsLineVM> ReturnsItems = [];
    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    ReturnsLineVM? SelectedLine;
    //ReturnsLineVM? LastScanned => ReturnsItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;
    //bool IsWeightDialogOpen = false;
    //decimal? ChangeWeight = null;

    protected override async Task OnInitializedAsync()
    {
        ActionGetReturnsItems = new AppAction<List<ReturnsLineVM>>
        {
            Name = "GetReturnsItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<ReturnsLineVM>>("/Packing/Returns/Items", new { OrderNumber = OrderNumber });
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
            Name = "SaveReturnsScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/Packing/Returns/SaveScan", ReturnsItems);
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
            await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);

            ItemRequest = ReturnsItems.Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
        }

        if (ReturnsItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadReturns()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);
    }

    private void SelectLine(ReturnsLineVM item)
    {
        if (SelectedLine?.LineSequenceNumber == item.LineSequenceNumber)
        {
            SelectedLine = null;
        }
        else
        {
            SelectedLine = item;
        }

        InvokeAsync(StateHasChanged);
    }

    private bool IsSelected(ReturnsLineVM row)
    {
        return SelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

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

            var line = ReturnsItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (SelectedLine == null ||
                     x.LineSequenceNumber == SelectedLine.LineSequenceNumber));

            if (line is null)
            {
                await Toast.Warning("Item not found in this Returns.");
                return;
            }

            var isOverScan = line.ScannedQuantity >= line.NSLineQuantityPacked;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {line.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / line.UoMRate;

            var remainingQty = line.NSLineQuantityPacked - line.ScannedQuantity;

            bool isExceed = scanQty > remainingQty;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {line.MaterialCode}.");
                return;
            }

            //decimal? weight = null;

            //if (ChangeWeight.HasValue)
            //{
            //    weight = ChangeWeight;
            //}
            //else if (!barcode.DefaultWeight.HasValue)
            //{
            //    if (IsWeightDialogOpen)
            //    {
            //        return;
            //    }

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

            line.ScannedQuantity += barcode.UoMRate / line.UoMRate;
            //line.ScannedWeight += weight ?? 0m;
            line.ScanCount++;

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
            var barcode = ItemBarcodes.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.MaterialBarcode) &&
                x.MaterialBarcode.Equals(scanned, StringComparison.OrdinalIgnoreCase));

            if (barcode is null)
            {
                await Toast.Warning($"Unknown barcode: {scanned}");
                return;
            }

            var line = ReturnsItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (SelectedLine == null ||
                     x.LineSequenceNumber == SelectedLine.LineSequenceNumber));

            if (line is null)
            {
                await Toast.Warning("Item not found in this Returns.");
                return;
            }

            var scanQty = barcode.UoMRate / line.UoMRate;

            bool isExceed = scanQty > line.ScannedQuantity;

            if (isExceed)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {line.MaterialCode}.");
                return;
            }

            //decimal? weight = null;

            //weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

            //if (!weight.HasValue || weight.Value == 0m)
            //{
            //    await Toast.Warning("Scan cancelled - no weight entered");
            //    return;
            //}

            line.ScannedQuantity -= barcode.UoMRate / line.UoMRate;
            //line.ScannedWeight -= weight ?? 0m;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        ReturnsItems = ReturnsItems.Where(x => x.NSLineQuantityPacked != 0)
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

    //async void ToggleWeight()
    //{
    //    ChangeWeight = await GetWeightAsync("", "");
    //}

    private bool IsActionPanelCollapsed;
    private void ToggleActionPanel()
    {
        IsActionPanelCollapsed = !IsActionPanelCollapsed;
    }

    private bool NegateQuantity;
    private void ToggleNegateQuantity()
    {
        NegateQuantity = !NegateQuantity;
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
}