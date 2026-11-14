using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.TransferOrder;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.Helpers.FormatHelper;

namespace Mobile.MAUI.Components.Pages.Receiving.TransferOrder;

public partial class TransferOrderItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }
    AppAction<List<TransferOrderLineVM>> ActionGetTOItems { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<TransferOrderLineVM> GoodTOItems = [];
    List<TransferOrderLineVM> BadTOItems = [];

    List<TransferOrderLineVM> TOItems = [];

    TransferOrderLineVM? GoodSelectedLine;
    TransferOrderLineVM? BadSelectedLine;
    //TransferOrderLineVM? LastScanned => TOItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    private int _scanCount;
    int ScanCount
    {
        get => _scanCount;
        set
        {
            _scanCount = value;
            if (value > 0) MarkScansDirty();
        }
    }
    bool SaveBtnDisabled => ScanCount == 0;
    int ActiveTabIndex { get; set; } = 0;

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
                GoodTOItems = (result.Data ?? []).Select(line => new TransferOrderLineVM
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

                BadTOItems = (result.Data ?? []).Select(line => new TransferOrderLineVM
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

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveTransferOrderScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/Receiving/TransferOrder/SaveScan", TOItems);
                return res;
            },
            OnSuccess = async (result) =>
            {
                if (!result.Success)
                {
                    await Toast.Error(result.ErrorMessage);
                    return;
                }

                MarkScansClean();
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

            var resolution = await BarcodeResolver.ResolveAsync(scanned);

            if (!resolution.IsFound)
            {
                if (resolution.IsFailure)
                {
                    await Toast.Error(resolution.Message);
                }
                else
                {
                    await Toast.Warning(resolution.Message);
                }

                return;
            }

            var barcode = resolution.Barcode!;

            var goodLine = GoodTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


            //if (NextScanIsBad)
            //{
                badLine = BadTOItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            //}
            //else
            //{
            //    badLine = BadTOItems.FirstOrDefault(x =>
            //        x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
            //        (BadSelectedLine == null ||
            //         x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            //}

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
                ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

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
                    weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                    if (!weight.HasValue || weight.Value == 0m)
                    {
                        await Toast.Warning("Scan cancelled - no weight entered");
                        return;
                    }

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
        TOItems = GoodTOItems
            .Where(g =>
            {
                var bad = BadTOItems.FirstOrDefault(b =>
                    b.LineSequenceNumber == g.LineSequenceNumber);

                var badQty = bad?.ScannedQuantity ?? 0;

                return g.ScannedQuantity > 0 ||
                        (g.ScannedQuantity + badQty) < g.NSLineQuantityReceived;
            })
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
                ScannedQuantity = RoundOfNearestHundredThousands(x.ScannedQuantity),
                ScannedWeight = x.ScannedWeight
            })
            .ToList();

        await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);

        await InvokeAsync(StateHasChanged);
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

    async void ToggleWeight()
    {
        ChangeWeight = await GetWeightAsync("", "");
    }

    async Task MoveScan(string scanned)
    {
        try
        {
            TransferOrderLineVM? badLine;
            TransferOrderLineVM? goodLine;

            var resolution = await BarcodeResolver.ResolveAsync(scanned);

            if (!resolution.IsFound)
            {
                if (resolution.IsFailure)
                {
                    await Toast.Error(resolution.Message);
                }
                else
                {
                    await Toast.Warning(resolution.Message);
                }

                return;
            }

            var barcode = resolution.Barcode!;

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

                ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

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

                decimal? weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                if (!weight.HasValue || weight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;
                var goodScannedWeight = weight ?? 0m    ;

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
            TransferOrderLineVM? badLine;
            TransferOrderLineVM? goodLine;

            var resolution = await BarcodeResolver.ResolveAsync(scanned);

            if (!resolution.IsFound)
            {
                if (resolution.IsFailure)
                {
                    await Toast.Error(resolution.Message);
                }
                else
                {
                    await Toast.Warning(resolution.Message);
                }

                return;
            }

            var barcode = resolution.Barcode!;

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

                ChangeWeight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                if (!ChangeWeight.HasValue || ChangeWeight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

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

                decimal? weight = await GetWeightAsync(barcode.MaterialName, barcode.UoMName);

                if (!weight.HasValue || weight.Value == 0m)
                {
                    await Toast.Warning("Scan cancelled - no weight entered");
                    return;
                }

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

    public async ValueTask DisposeAsync()
    {
        MarkScansClean();
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

    private bool NextScanIsBad => ScanState == ToggleState.Bad;
    #endregion
}