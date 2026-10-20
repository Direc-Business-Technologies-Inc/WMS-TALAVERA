using Microsoft.JSInterop;
using Mobile.MAUI.Components.Reusables;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Radzen;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.ItemFulfillment;
using System.Text.Json;
using static Mobile.MAUI.Components.Reusables.WeightOptionDialog;
using static Mobile.MAUI.Helpers.FormatHelper;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.Receiving.Returns;

public partial class TOxReturnxItemFulfillmentItemView : IAsyncDisposable
{
    [Parameter]
    public int NetsuiteOrderInternalId { get; set; }

    [Parameter]
    public string TOOrderNumber { get; set; }

    [Parameter]
    public string OrderNumber { get; set; }

    string BackPath => $"/receiving/returns/itemfulfillment/{NetsuiteOrderInternalId}/{TOOrderNumber}";

    [Inject] DialogService _dialogService { get; set; }

    AppAction<List<TOxItemFulfillmentLineVM>> ActionGetReturnsItems { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<TOxItemFulfillmentLineVM> ReturnsItems = [];
    List<TOxItemFulfillmentLineVM> FilteredReturnsItems = [];
    // Snapshot built by SaveScan and posted by ActionSaveScan. Kept separate from
    // ReturnsItems so a save never rewrites the scan state it is reading from.
    List<TOxItemFulfillmentLineVM> SavePayload = [];
    TOxItemFulfillmentLineVM? LastScanned;

    TOxItemFulfillmentLineVM? SelectedLine;
    //TOxItemFulfillmentLineVM? LastScanned => ReturnsItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

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
    bool IsWeightDialogOpen = false;
    decimal? ChangeWeight = null;

    ReceiveMode ReceiveByWeightMode = ReceiveMode.WithoutWeight;

    int UserId = 0;
    string Remarks = string.Empty;

    private string SearchText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        ActionGetReturnsItems = new AppAction<List<TOxItemFulfillmentLineVM>>
        {
            Name = "GetReturnsItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<TOxItemFulfillmentLineVM>>("/Receiving/Returns/ItemFulfillment/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                // The returns line query joins the item's preferred bin and the vendor's bin
                // assignment, so one transaction line can fan out into several rows with the
                // same LineSequenceNumber. That number IS the NetSuite orderLine, so keeping
                // two rows for it makes every save post a duplicate orderLine and NetSuite
                // rejects the document with [DUPLICATE_KEYS]. Collapse to one row per line,
                // preferring the copy that carries a bin because the receipt builder resolves
                // the bin from the row.
                ReturnsItems = result.Data
                .GroupBy(line => line.LineSequenceNumber)
                .Select(g => g
                    .OrderByDescending(line => line.NetsuiteMaterialPrefferedBinId != 0)
                    .First())
                .Select(line => new TOxItemFulfillmentLineVM
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

                ApplySearch();
                await InvokeAsync(StateHasChanged);
            },
        };

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveReturnsScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/Receiving/Returns/SaveScan", new { 
                    PostReturn = SavePayload, 
                    TONetsuiteOrderInternalId = NetsuiteOrderInternalId, 
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
            ReceiveByWeightMode = await SelectWeightOption();

            await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);

            string? userAuth = await SecureStorage.GetAsync("UserAuth");
            if (userAuth is not null)
            {
                var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);

                UserId = auth.NetsuiteEmployeeInternalId;
            }
        }
    }

    async Task LoadReturns()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);
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
            FilteredReturnsItems = ReturnsItems.ToList();
            return;
        }

        var search = SearchText.Trim();

        FilteredReturnsItems = ReturnsItems
            .Where(row => MatchesSearch(row, search))
            .ToList();
    }

    private static bool MatchesSearch(TOxItemFulfillmentLineVM row, string search)
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

    private async void SelectLine(TOxItemFulfillmentLineVM item)
    {
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

                var result = await Dialog.OpenAsync<ManualEntryDialog>(
                    "Manual Entry",
                    new Dictionary<string, object>
                    {
                        { "ItemName", item.MaterialName },
                        { "PlannedQty", item.NSLineQuantityReceived },
                        { "GoodQty", item.ScannedQuantity },
                        { "ShowBad", 0},
                        { "ShowPhysical", 0}
                    },
                    new DialogOptions
                    {
                        ShowClose = true,
                    });

                if (result is ManualEntryDialog.ManualEntryResult entry)
                {
                    ScanCount = 1;
                    item.ScannedQuantity = entry.GoodQty;
                }
            }
            finally
            {
                IsWeightDialogOpen = false;
            }
        }

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

    private bool IsSelected(TOxItemFulfillmentLineVM row)
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

            var line = ReturnsItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (SelectedLine == null ||
                     x.LineSequenceNumber == SelectedLine.LineSequenceNumber));

            if (line is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var remainingQty = line.NSLineQuantityReceived - line.ScannedQuantity;

            // Remaining is 0 for a line that is already fully received, including 0 >= 0
            // before anything was scanned for it, so the old
            // "ScannedQuantity >= NSLineQuantityReceived" guard reported over-scanning before
            // the first scan. Report what is actually true and let the quantity comparison
            // below reject a scan that does not fit in the remainder.
            if (remainingQty <= 0)
            {
                await Toast.Warning(line.NSLineQuantityReceived <= 0
                    ? $"{line.MaterialCode} has no remaining quantity to receive."
                    : $"Over-scanning item: {line.MaterialCode}.");

                return;
            }

            var scanQty = barcode.UoMRate / line.UoMRate;

            if (scanQty > remainingQty)
            {
                await Toast.Warning($"Scan quantity exceeds remaining quantity for item: {line.MaterialCode}.");
                return;
            }

            decimal? weight = null;

            if (IsWeightDialogOpen)
            {
                return;
            }

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

            line.ScannedQuantity += barcode.UoMRate / line.UoMRate;
            line.ScannedWeight += weight ?? 0m;
            line.ScanCount++;

            LastScanned = line;

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

            line.ScannedQuantity -= barcode.UoMRate / line.UoMRate;
            line.ScannedWeight -= weight ?? 0m;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        var remainingByLine = ReturnsItems
            .Select(g =>
            {
                var remaining = g.NSLineQuantityReceived - g.ScannedQuantity;

                return new
                {
                    LineSequenceNumber = g.LineSequenceNumber,
                    RemainingToScan = Math.Max(0, remaining)
                };
            })
            .ToList();

        var missingItems = remainingByLine
            .Where(x => x.RemainingToScan > 0)
            .ToList();

        List<TOxItemFulfillmentLineVM> missingReturns = [];

        if (missingItems.Any())
        {
            var totalMissing = missingItems.Sum(x => x.RemainingToScan);

            var warning = $"Warning: {totalMissing} item(s) still need to be scanned. " +
                          "These items will be tagged as missing.";

            var confirm = await _dialogService.Confirm(warning, "Missing Items?");

            // Proceed = post the unscanned remainder tagged as missing (category 5).
            // Cancel = post only what was scanned and leave the remainder open, so the
            // operator can return to the document later.
            if (confirm is true)
            {
                missingReturns = missingItems
                    .Select(m =>
                    {
                        var source = ReturnsItems.FirstOrDefault(g =>
                            g.LineSequenceNumber == m.LineSequenceNumber);

                        if (source == null)
                            return null;

                        return new TOxItemFulfillmentLineVM
                        {
                            NetsuiteOrderInternalId = source.NetsuiteOrderInternalId,
                            OrderNumber = source.OrderNumber,
                            OrderType = source.OrderType,
                            OrderStatus = source.OrderStatus,
                            TransferCategory = source.TransferCategory,

                            NetsuiteFromLocationInternalId = source.NetsuiteFromLocationInternalId,
                            NetsuiteToLocationInternalId = source.NetsuiteToLocationInternalId,

                            NetsuiteFromSubsidiaryInternalId = source.NetsuiteFromSubsidiaryInternalId,
                            NetsuiteSubsidiaryDefaultBOInternalId = source.NetsuiteSubsidiaryDefaultBOInternalId,
                            NetsuiteToSubsidiaryInternalId = source.NetsuiteToSubsidiaryInternalId,

                            LocationName = source.LocationName,
                            LocationUsedBin = source.LocationUsedBin,

                            LineSequenceNumber = source.LineSequenceNumber,
                            TransactionLineType = source.TransactionLineType,

                            NetsuiteMaterialInternalId = source.NetsuiteMaterialInternalId,
                            MaterialCode = source.MaterialCode,
                            MaterialName = source.MaterialName,
                            MaterialWeight = source.MaterialWeight,
                            LineQuantity = source.LineQuantity,
                            LineQuantityReceived = source.LineQuantityReceived,
                            NetsuiteUoMInternalId = source.NetsuiteUoMInternalId,
                            UoMName = source.UoMName,
                            UoMRate = source.UoMRate,

                            ScanCount = 0,

                            // Mark this item as missing
                            IsBad = false,
                            IsMissing = true,

                            // The missing quantity is what remains unscanned
                            ScannedQuantity = RoundOfNearestHundredThousands(
                                m.RemainingToScan),

                            ScannedWeight = 0
                        };
                    })
                    .Where(x => x != null)
                    .ToList()!;
            }
        }

        // Snapshot the payload instead of rewriting ReturnsItems. The old code rebuilt
        // ReturnsItems from its own output, which dropped IsMissing and concatenated a
        // stale MissingReturnsItems on every attempt: the second save posted the same
        // orderLine in both buckets and NetSuite rejected it with [DUPLICATE_KEYS].
        // Grouping by (LineSequenceNumber, IsMissing) keeps one entry per orderLine per
        // bucket, which is what the receipt payload requires.
        SavePayload = ReturnsItems.Where(x => x.NSLineQuantityReceived != 0)
            .Select(x => new TOxItemFulfillmentLineVM
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
                IsMissing = x.IsMissing,
                ScannedQuantity = RoundOfNearestHundredThousands(x.ScannedQuantity),
                ScannedWeight = x.ScannedWeight
            })
            .Concat(missingReturns)
            .GroupBy(x => (x.LineSequenceNumber, x.IsMissing))
            .Select(g => g.First())
            .ToList();

        if (SavePayload.Count == 0)
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

                await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);
            }
        }
        catch (Exception e)
        {
            await Toast.Error($"Error: {e.Message}");
        }


        await InvokeAsync(StateHasChanged);
    }

    async void ToggleWeight()
    {
        ChangeWeight = await GetWeightAsync("", "");
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

    private bool ManualEntry = false;
    private bool ValidManual => IsValidForManualEntry(LastScanned);

    private bool IsValidForManualEntry(TOxItemFulfillmentLineVM? row)
    {
        return row is not null
            && (row.NSLineQuantityPacked >= GlobalState.ManualEntryThreshold
                || row.NSLineQuantityReceived >= GlobalState.ManualEntryThreshold
                || row.ScannedQuantity >= GlobalState.ManualEntryThreshold)
            && row.ScanCount >= GlobalState.ManualEntryThreshold;
    }

    private void ToggleManualEntry()
    {
        ManualEntry = !ManualEntry;
        NegateQuantity = false;
    }

    private async Task Refresh()
    {
        // Snapshot current scanned state before refreshing
        var scannedState = ReturnsItems
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
                });

        // Reload PO items
        await ActionFactory.ExecuteAppActionAsync(ActionGetReturnsItems);

        // Restore scanned state
        foreach (var item in ReturnsItems)
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
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        MarkScansClean();
        BroadcastService.BroadcastReceived -= HandleItemScan;
    }
}
