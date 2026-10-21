using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.InventoryCounting;
using Mobile.MAUI.Components.Reusables;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.InventoryCounting;

public partial class InventoryCountingItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    AppAction<List<InventoryCountingLineVM>> ActionGetICItems { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<InventoryCountingLineVM> GoodICItems = [];
    List<InventoryCountingLineVM> BadICItems = [];
    List<InventoryCountingLineVM> FilteredGoodICItems = [];
    List<InventoryCountingLineVM> FilteredBadICItems = [];

    List<InventoryCountingLineVM> ICItems = [];

    InventoryCountingLineVM? GoodSelectedLine;
    InventoryCountingLineVM? BadSelectedLine;
    InventoryCountingLineVM? LastScanned;

    private string SearchText { get; set; } = string.Empty;

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
    int ActiveTabIndex { get; set; } = 0;

    bool HasCountedLines => GoodICItems.Any(x => x.IsCounted) || BadICItems.Any(x => x.IsCounted);
    bool SaveBtnDisabled => !HasCountedLines;
    bool MoveOn = false;
    bool IsWeightDialogOpen = false;

    decimal? ChangeWeight = null;
    protected override async Task OnInitializedAsync()
    {
        ActionGetICItems = new AppAction<List<InventoryCountingLineVM>>
        {
            Name = "GetICItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<InventoryCountingLineVM>>("/InventoryCounting/Items", new { OrderNumber = OrderNumber });
                return res;
            },
            OnSuccess = async (result) =>
            {
                var data = result.Data ?? [];

                GoodICItems = data.Select(line => new InventoryCountingLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteSubsidiaryInternalId = line.NetsuiteSubsidiaryInternalId,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    LineQuantity = line.LineQuantity,

                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    NetsuiteInventoryDetailInternalId = line.NetsuiteInventoryDetailInternalId,
                    CountedQuantity = line.CountedQuantity,
                    GoodCountedQuantity = line.GoodCountedQuantity,
                    BadCountedQuantity = line.BadCountedQuantity,

                    ScanCount = 0,
                    ScannedQuantity = line.GoodCountedQuantity,
                    IsBad = false,
                    IsCounted = false,
                }).ToList();

                BadICItems = data.Select(line => new InventoryCountingLineVM
                {
                    NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
                    OrderNumber = line.OrderNumber,
                    OrderType = line.OrderType,
                    OrderStatus = line.OrderStatus,

                    NetsuiteSubsidiaryInternalId = line.NetsuiteSubsidiaryInternalId,

                    LineSequenceNumber = line.LineSequenceNumber,
                    TransactionLineType = line.TransactionLineType,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    LineQuantity = line.LineQuantity,

                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    NetsuiteInventoryDetailInternalId = line.BadInventoryDetailId,
                    CountedQuantity = line.CountedQuantity,
                    GoodCountedQuantity = line.GoodCountedQuantity,
                    BadCountedQuantity = line.BadCountedQuantity,

                    ScanCount = 0,
                    ScannedQuantity = line.BadCountedQuantity,
                    IsBad = true,
                    IsCounted = false,
                }).ToList();

                ApplySearch();
                await InvokeAsync(StateHasChanged);
            },
            OnFailure = async (result) =>
            {
                await Toast.Error($"Unable to load the count: {result.ErrorMessage}");
            },
        };

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveInventoryCountingScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/InventoryCounting/SaveScan", ICItems);
                return res;
            },
            OnSuccess = async (result) =>
            {
                MarkScansClean();
                await Toast.Success("Scanned items saved sucessfully");
                NavManager.NavigateTo("/inventorycounting");
            }
        };

        BroadcastService.BroadcastReceived += HandleItemScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ActionFactory.ExecuteAppActionAsync(ActionGetICItems);
        }

        if (GoodICItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
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
            FilteredGoodICItems = GoodICItems.ToList();
            FilteredBadICItems = BadICItems.ToList();
            return;
        }

        var search = SearchText.Trim();

        FilteredGoodICItems = GoodICItems
            .Where(row => MatchesSearch(row, search))
            .ToList();

        FilteredBadICItems = BadICItems
            .Where(row => MatchesSearch(row, search))
            .ToList();
    }

    private static bool MatchesSearch(InventoryCountingLineVM row, string search)
    {
        return ContainsIgnoreCase(row.MaterialCode, search)
            || ContainsIgnoreCase(row.MaterialName, search)
            || ContainsIgnoreCase(row.UoMName, search);
    }

    private static bool ContainsIgnoreCase(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    private async Task Refresh()
    {
        // Keep in-progress scans; the reload only refreshes planned quantities and detail ids.
        var scannedState = GoodICItems
            .Concat(BadICItems)
            .GroupBy(x => new { x.LineSequenceNumber, x.NetsuiteMaterialInternalId, x.IsBad })
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ScannedQuantity = g.First().ScannedQuantity,
                    ScanCount = g.First().ScanCount,
                    IsCounted = g.First().IsCounted
                });

        await ActionFactory.ExecuteAppActionAsync(ActionGetICItems);

        foreach (var item in GoodICItems.Concat(BadICItems))
        {
            var key = new { item.LineSequenceNumber, item.NetsuiteMaterialInternalId, item.IsBad };

            if (scannedState.TryGetValue(key, out var scanned))
            {
                item.ScannedQuantity = scanned.ScannedQuantity;
                item.ScanCount = scanned.ScanCount;
                item.IsCounted = scanned.IsCounted;
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    private async void SelectGoodLine(InventoryCountingLineVM item)
    {
        if (ManualEntry)
        {
            if (!IsValidForManualEntry(item))
            {
                await Toast.Warning("Scan the same SKU 11 times before using Manual Entry.");
                return;
            }

            await OpenManualEntry(item);
            GoodSelectedLine = item;
            await InvokeAsync(StateHasChanged);
            return;
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

    private bool IsSelectedGood(InventoryCountingLineVM row)
    {
        return GoodSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    private async void SelectBadLine(InventoryCountingLineVM item)
    {
        if (ManualEntry)
        {
            if (!IsValidForManualEntry(item))
            {
                await Toast.Warning("Scan the same SKU 11 times before using Manual Entry.");
                return;
            }

            await OpenManualEntry(item);
            BadSelectedLine = item;
            await InvokeAsync(StateHasChanged);
            return;
        }

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

    private bool IsSelectedBad(InventoryCountingLineVM row)
    {
        return BadSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity) return;

            InventoryCountingLineVM? badLine;

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

            var goodLine = GoodICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


            if (NextScanIsBad)
            {
                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }
            else
            {
                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this IC.");
                return;
            }

            if (NextScanIsBad)
            {
                badLine.ScannedQuantity += barcode.UoMRate / badLine.UoMRate;
                badLine.ScanCount++;
                badLine.IsCounted = true;
                LastScanned = badLine;
            }
            else
            {
                goodLine.ScannedQuantity += barcode.UoMRate / goodLine.UoMRate;
                goodLine.ScanCount++;
                goodLine.IsCounted = true;
                LastScanned = goodLine;
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
        ICItems = GoodICItems
            .Concat(BadICItems)
            .Select(x => new InventoryCountingLineVM
            {
                NetsuiteOrderInternalId = x.NetsuiteOrderInternalId,
                OrderNumber = x.OrderNumber,
                OrderType = x.OrderType,
                OrderStatus = x.OrderStatus,

                NetsuiteSubsidiaryInternalId = x.NetsuiteSubsidiaryInternalId,

                LineSequenceNumber = x.LineSequenceNumber,
                TransactionLineType = x.TransactionLineType,

                NetsuiteMaterialInternalId = x.NetsuiteMaterialInternalId,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                LineQuantity = x.LineQuantity,

                UoMName = x.UoMName,
                UoMRate = x.UoMRate,

                NetsuiteInventoryDetailInternalId = x.NetsuiteInventoryDetailInternalId,

                ScanCount = x.ScanCount,
                ScannedQuantity = x.ScannedQuantity,
                IsBad = x.IsBad,
                IsCounted = x.IsCounted,
            })
            .ToList();


        await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);

        await InvokeAsync(StateHasChanged);
    }

    void ToggleMove()
    {
        MoveOn = !MoveOn;
        if (MoveOn)
        {
            ManualEntry = false;
            NegateQuantity = false;
        }
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
        if (NegateQuantity)
        {
            ManualEntry = false;
        }
    }

    private bool ManualEntry = false;
    private bool ValidManual => IsValidForManualEntry(LastScanned);

    private bool IsValidForManualEntry(InventoryCountingLineVM? row)
    {
        return row is not null
            && (row.LineQuantity >= GlobalState.ManualEntryThreshold
                || row.ScannedQuantity >= GlobalState.ManualEntryThreshold)
            && row.ScanCount >= GlobalState.ManualEntryThreshold;
    }

    private void ToggleManualEntry()
    {
        ManualEntry = !ManualEntry;
        if (ManualEntry)
        {
            MoveOn = false;
        }
        NegateQuantity = false;
    }

    private async Task OpenManualEntry(InventoryCountingLineVM item)
    {
        var goodLine = GoodICItems.FirstOrDefault(x =>
            x.LineSequenceNumber == item.LineSequenceNumber &&
            x.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);
        var badLine = BadICItems.FirstOrDefault(x =>
            x.LineSequenceNumber == item.LineSequenceNumber &&
            x.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

        if (goodLine is null || badLine is null)
        {
            await Toast.Warning("Item not found in this IC.");
            return;
        }

        var result = await Dialog.OpenAsync<ManualEntryDialog>(
            "Manual Entry",
            new Dictionary<string, object>
            {
                { "ItemName", item.MaterialName },
                { "PlannedQty", item.LineQuantity },
                { "GoodQty", goodLine.ScannedQuantity },
                { "BadQty", badLine.ScannedQuantity },
                { "ShowBad", 1 },
                { "ShowMissing", 0 },
                { "ShowPhysical", 0 },
                { "AllowZero", true },
                { "EnforcePlannedLimit", false }
            },
            new DialogOptions { ShowClose = true });

        if (result is ManualEntryDialog.ManualEntryResult entry)
        {
            goodLine.ScannedQuantity = entry.GoodQty;
            badLine.ScannedQuantity = entry.BadQty;
            goodLine.IsCounted = true;
            badLine.IsCounted = true;
            ScanCount = Math.Max(ScanCount, 1);
        }
    }

    async Task MoveScan(string scanned)
    {
        try
        {
            InventoryCountingLineVM? badLine;
            InventoryCountingLineVM? goodLine;

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
                goodLine = GoodICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this IC.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;

                goodLine.ScannedQuantity += badScannedQuantity;
                badLine.ScanCount++;
                badLine.IsCounted = true;
                goodLine.IsCounted = true;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;

                badLine.ScannedQuantity += goodScannedQuantity;

                goodLine.ScanCount++;
                goodLine.IsCounted = true;
                badLine.IsCounted = true;
            }

            ScanCount++;

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
            InventoryCountingLineVM? badLine;
            InventoryCountingLineVM? goodLine;

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
                goodLine = GoodICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (BadSelectedLine == null ||
                 x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));

                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (BadSelectedLine == null ||
                     x.LineSequenceNumber == BadSelectedLine.LineSequenceNumber));
            }
            else
            {
                goodLine = GoodICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                (GoodSelectedLine == null ||
                 x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));


                badLine = BadICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                    (GoodSelectedLine == null ||
                     x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            }

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            if (ActiveTabIndex == 1)
            {
                if (badLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                var badScannedQuantity = barcode.UoMRate / badLine.UoMRate;

                if (badLine.ScannedQuantity < badScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                badLine.ScannedQuantity -= badScannedQuantity;

                badLine.ScanCount++;
                badLine.IsCounted = true;
            }
            else
            {
                if (goodLine.ScannedQuantity == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                var goodScannedQuantity = barcode.UoMRate / goodLine.UoMRate;

                if (goodLine.ScannedQuantity < goodScannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                goodLine.ScannedQuantity -= goodScannedQuantity;

                goodLine.ScanCount++;
                goodLine.IsCounted = true;
            }

            ScanCount++;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
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
