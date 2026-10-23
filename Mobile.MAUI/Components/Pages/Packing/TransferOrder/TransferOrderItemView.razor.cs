using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.TransferOrder;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.Helpers.FormatHelper;
using Mobile.MAUI.Components.Reusables;

namespace Mobile.MAUI.Components.Pages.Packing.TransferOrder;

public partial class TransferOrderItemView : IAsyncDisposable
{
    [Parameter]
    public string OrderNumber { get; set; }

    AppAction<List<TransferOrderLineVM>> ActionGetTOItems { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction<bool> ActionSaveScan { get; set; }

    List<TransferOrderLineVM> GoodTOItems = [];
    List<TransferOrderLineVM> BadTOItems = [];

    // Closed / fully-shipped lines: display-only, never scannable. A closed
    // orderLine is exactly what crashes the !transform/itemFulfillment save, so
    // these are kept off GoodTOItems/BadTOItems (and therefore out of ItemRequest
    // and every scan handler).
    List<TransferOrderLineVM> DoneTOItems = [];

    // Backordered lines are built from the raw response, not from GoodTOItems.
    // NetSuite closes a line when it backorders the remainder, so a backordered
    // line is also IsCompleted and would otherwise land in DoneTOItems and vanish
    // from the BackOrdered tab.
    List<TransferOrderLineVM> BackOrderedTOItems = [];


    List<TransferOrderLineVM> FilteredGoodTOItems = [];
    List<TransferOrderLineVM> FilteredBadTOItems = [];
    List<TransferOrderLineVM> FilteredDoneTOItems = [];
    List<TransferOrderLineVM> FilteredBackOrderedTOItems = [];

    TransferOrderLineVM? LastScanned;

    List<TransferOrderLineVM> TOItems = [];

    TransferOrderLineVM? GoodSelectedLine;
    TransferOrderLineVM? BadSelectedLine;

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

    private string SearchText { get; set; } = string.Empty;

    // Vendor gate. The vendor barcode printed under the Vendor field on the STR
    // encodes the vendor NAME as printed. It is resolved server-side against the
    // order's own line vendors (custcol_dbti_vendor) before the id is used.
    // Until it is scanned, every scan is a vendor scan and the line lists stay
    // empty.
    int? VendorId;
    string VendorName = string.Empty;

    // Guards the vendor-resolution round trip. While it is in flight the operator's
    // scans are swallowed: a double-fired scan or a re-scan during a slow network
    // call must not run a second ResolveVendorScan whose ClearVendorLines/Set
    // sequence would wipe or overwrite the state the first call is about to set,
    // leaving the operator scanning against a vendor they did not resolve.
    // The flag must also span the follow-up item load (ResolveVendorScan awaits
    // ActionGetTOItems before clearing it): a second resolution cannot start until
    // the first vendor's lines have been applied, otherwise two vendors' line sets
    // can interleave in the same lists.
    bool _resolvingVendor;

    // Set only for the Save-now path of ChangeVendor: the shared save action
    // navigates away by default, but a vendor switch must stay on this screen and
    // continue only when the save succeeded.
    bool _switchingVendor;

    // Backorder notification
    bool HasBackorderItems
    {
        get
        {
            return BackOrderedTOItems.Count > 0;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        ActionGetTOItems = new AppAction<List<TransferOrderLineVM>>
        {
            Name = "GetTOItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<TransferOrderLineVM>>("/Packing/TransferOrder/Items", new { OrderNumber = OrderNumber, VendorId = VendorId });
                return res;
            },
            OnSuccess = async (result) =>
            {
                var source = result.Data ?? [];

                if (VendorId is not null)
                {
                    // Keep the name the resolver matched against the printed barcode:
                    // the line's display name may be a different vendor name field.
                    if (string.IsNullOrWhiteSpace(VendorName))
                    {
                        VendorName = source.Select(line => line.VendorName)
                            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty;
                    }

                    if (source.Count == 0)
                        await Toast.Warning("No open items for this vendor on this transfer order.");
                }

                var openLines = source.Where(line => !line.IsCompleted).ToList();

                BackOrderedTOItems = source.Where(line => line.LineQuantityBackOrdered > 0).ToList();

                GoodTOItems = openLines.Select(line => new TransferOrderLineVM
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
                    PreferredBinQuantityAvailableGood = line.PreferredBinQuantityAvailableGood,

                    NetsuiteMaterialVendorAssignedBin = line.NetsuiteMaterialVendorAssignedBin,
                    VendorAssignedBinQuantityAvailableGood = line.VendorAssignedBinQuantityAvailableGood,

                    LocationItemQuantityAvailable = line.LocationItemQuantityAvailable,

                    LocationItemQuantityAvailableGood = line.LocationItemQuantityAvailableGood,

                    LineQuantity = line.LineQuantity,
                    LineQuantityPacked = line.LineQuantityPacked,
                    LineQuantityBackOrdered = line.LineQuantityBackOrdered,

                    IsClosed = line.IsClosed,
                    QuantityAlreadyShipped = line.QuantityAlreadyShipped,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,
                    IsBad = false,
                }).ToList() ?? [];

                BadTOItems = openLines.Select(line => new TransferOrderLineVM
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
                    LineQuantityBackOrdered = line.LineQuantityBackOrdered,

                    NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
                    MaterialCode = line.MaterialCode,
                    MaterialName = line.MaterialName,
                    MaterialWeight = line.MaterialWeight,

                    NetsuiteMaterialPrefferedBinId = line.NetsuiteMaterialPrefferedBinId,
                    PreferredBinQuantityAvailableBad = line.PreferredBinQuantityAvailableBad,

                    NetsuiteMaterialVendorAssignedBin = line.NetsuiteMaterialVendorAssignedBin,
                    VendorAssignedBinQuantityAvailableBad = line.VendorAssignedBinQuantityAvailableBad,

                    LocationItemQuantityAvailable = line.LocationItemQuantityAvailable,
                    LocationItemQuantityAvailableBad = line.LocationItemQuantityAvailableBad,


                    LineQuantity = line.LineQuantity,
                    LineQuantityPacked = line.LineQuantityPacked,

                    IsClosed = line.IsClosed,
                    QuantityAlreadyShipped = line.QuantityAlreadyShipped,

                    NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
                    UoMName = line.UoMName,
                    UoMRate = line.UoMRate,

                    ScanCount = 0,
                    ScannedQuantity = 0,
                    ScannedWeight = 0,
                    IsBad = true,
                }).ToList() ?? [];

                // Closed / fully-shipped lines: read-only DONE list. Excluded from
                // GoodTOItems/BadTOItems so ItemRequest and every scan handler skip them.
                DoneTOItems = source.Where(line => line.IsCompleted && line.LineQuantityBackOrdered <= 0).Select(line => new TransferOrderLineVM
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
                    LineQuantityBackOrdered = line.LineQuantityBackOrdered,

                    IsClosed = line.IsClosed,
                    QuantityAlreadyShipped = line.QuantityAlreadyShipped,

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
            OnFailure = async (result) =>
            {
                await Toast.Error(result.ErrorMessage);
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

                MarkScansClean();
                await Toast.Success("Scanned items saved sucessfully");

                // A vendor switch stays on this screen; only the normal Save
                // leaves the packing list.
                if (_switchingVendor) return;

                NavManager.NavigateTo("/packing");
            }
        };

        BroadcastService.BroadcastReceived += HandleItemScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && VendorId is not null)
        {
            await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);
        }
    }

    async Task LoadTransferOrder()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);
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
            FilteredGoodTOItems = GoodTOItems.ToList();
            FilteredBadTOItems = BadTOItems.ToList();
            FilteredDoneTOItems = DoneTOItems.ToList();
            FilteredBackOrderedTOItems = BackOrderedTOItems.ToList();
            return;
        }

        var search = SearchText.Trim();

        FilteredGoodTOItems = GoodTOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();

        FilteredBadTOItems = BadTOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();

        FilteredDoneTOItems = DoneTOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();

        FilteredBackOrderedTOItems = BackOrderedTOItems
            .Where(row => MatchesSearch(row, search))
            .ToList();
    }

    private static bool MatchesSearch(TransferOrderLineVM row, string search)
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

    private async void SelectGoodLine(TransferOrderLineVM item)
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

    private bool IsSelectedGood(TransferOrderLineVM row)
    {
        return GoodSelectedLine?.LineSequenceNumber
            == row.LineSequenceNumber;
    }

    private async void SelectBadLine(TransferOrderLineVM item)
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

        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenManualEntry(TransferOrderLineVM item)
    {
        var goodLine = GoodTOItems.FirstOrDefault(x =>
            x.LineSequenceNumber == item.LineSequenceNumber &&
            x.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);
        var badLine = BadTOItems.FirstOrDefault(x =>
            x.LineSequenceNumber == item.LineSequenceNumber &&
            x.NetsuiteMaterialInternalId == item.NetsuiteMaterialInternalId);

        if (goodLine is null || badLine is null)
        {
            await Toast.Warning("Item not found in this TO.");
            return;
        }

        var result = await Dialog.OpenAsync<ManualEntryDialog>(
            "Manual Entry",
            new Dictionary<string, object>
            {
                { "ItemName", item.MaterialName },
                { "PlannedQty", item.PackableQuantity },
                { "GoodQty", goodLine.ScannedQuantity },
                { "BadQty", badLine.ScannedQuantity },
                { "ShowMissing", 1 }
            },
            new DialogOptions { ShowClose = true });

        if (result is ManualEntryDialog.ManualEntryResult entry)
        {
            goodLine.ScannedQuantity = entry.GoodQty;
            badLine.ScannedQuantity = entry.BadQty;
            ScanCount = Math.Max(ScanCount, 1);
        }
    }

    private bool IsSelectedBad(TransferOrderLineVM row)
    {
        return BadSelectedLine?.LineSequenceNumber == row.LineSequenceNumber;
    }

    /// <summary>
    ///     Resolves the scanned vendor barcode to the vendor on this transfer
    ///     order. The barcode carries the vendor NAME (the same value printed
    ///     under the Vendor field), not an internal id, so the server matches it
    ///     against the order's own line vendors and returns the id to filter on.
    /// </summary>
    private async Task ResolveVendorScan(string scanned)
    {
        var value = scanned.Trim();

        // The document barcode is the tranid. Scanning it here is harmless — the
        // document is already open — so point at the next step instead of failing.
        if (string.Equals(value, (OrderNumber ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await Toast.Warning("That is the document barcode — this document is already open. Now scan the vendor barcode printed under the Vendor field.");
            return;
        }

        var result = await Client.Post<VendorLookupVM>(
            "/Packing/TransferOrder/ResolveVendor",
            new { OrderNumber = OrderNumber, VendorBarcode = value });

        if (!result.Success || result.Data is null)
        {
            await Toast.Warning(string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? $"'{value}' is not a vendor on this transfer order."
                : result.ErrorMessage);

            return;
        }

        ClearVendorLines();

        VendorId = result.Data.Id;
        VendorName = result.Data.Name;

        await InvokeAsync(StateHasChanged);
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);
    }

    private async Task ChangeVendor()
    {
        if (ScanCount > 0)
        {
            var saveNow = await Dialog.Confirm(
                $"You have {ScanCount} unsaved scan(s). Save them before changing vendor?",
                "Change Vendor",
                new ConfirmOptions { OkButtonText = "Save now", CancelButtonText = "Discard" });

            if (saveNow is null)
            {
                return;
            }

            if (saveNow == true)
            {
                _switchingVendor = true;
                try
                {
                    await ActionFactory.ExecuteAppActionAsync(ActionSaveScan);
                }
                finally
                {
                    _switchingVendor = false;
                }

                // ExecuteAppActionAsync absorbs exceptions into Result; branch on
                // the result, not a try/catch. A failed save keeps the gate on the
                // current vendor with its scans intact.
                if (ActionSaveScan.Result is not { Success: true })
                {
                    return;
                }
            }
            else
            {
                var discard = await Dialog.Confirm(
                    $"Discard {ScanCount} unsaved scan(s)?",
                    "Discard Scans",
                    new ConfirmOptions { OkButtonText = "Discard", CancelButtonText = "Cancel" });

                if (discard != true)
                {
                    return;
                }
            }
        }

        VendorId = null;
        VendorName = string.Empty;
        ClearVendorLines();

        await InvokeAsync(StateHasChanged);
    }

    private void ClearVendorLines()
    {
        GoodTOItems = [];
        BadTOItems = [];
        DoneTOItems = [];
        BackOrderedTOItems = [];
        TOItems = [];

        FilteredGoodTOItems = [];
        FilteredBadTOItems = [];
        FilteredDoneTOItems = [];
        FilteredBackOrderedTOItems = [];

        LastScanned = null;
        ActiveTabIndex = 0;
        ScanCount = 0;
        MarkScansClean();
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {
            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

            // Vendor gate. Checked before the ScanState gate on purpose: while the
            // gate is open the operator's scan target is the vendor barcode, not an
            // item, regardless of which Good/Bad state the toggle is in.
            if (_resolvingVendor) return;

            if (VendorId is null)
            {
                _resolvingVendor = true;
                await InvokeAsync(StateHasChanged);
                try
                {
                    await ResolveVendorScan(scanned);
                }
                finally
                {
                    _resolvingVendor = false;
                }

                return;
            }

            if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity) return;

            TransferOrderLineVM? badLine;

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
            //        (GoodSelectedLine == null ||
            //         x.LineSequenceNumber == GoodSelectedLine.LineSequenceNumber));
            //}

            if (goodLine is null)
            {
                await Toast.Warning("Item not found in this PO.");
                return;
            }

            var badlineTotal = badLine.ScannedQuantity;
            var goodLineTotal = goodLine.ScannedQuantity;

            // PackableQuantity is in the same display unit ScannedQuantity
            // accumulates in (barcode rate / line rate). NSLineQuantityRemaining
            // is raw stock units - comparing the two let a BOX12 line accept 12x
            // its remaining quantity (2026-10-23 over-scan report).
            var isOverScan = badlineTotal + goodLineTotal >= goodLine.PackableQuantity;

            if (isOverScan)
            {
                await Toast.Warning($"Over-scanning item: {goodLine.MaterialCode}.");
                return;
            }

            var scanQty = barcode.UoMRate / goodLine.UoMRate;

            var remainingQty = goodLine.PackableQuantity - (goodLine.ScannedQuantity + (badLine?.ScannedQuantity ?? 0));

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

                LastScanned = badLine;
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

                LastScanned = goodLine;
            }

            await RevealLastScanned();

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
        TOItems = GoodTOItems
            // The server now owns the authoritative open-line set (save-time
            // reconciliation), so the client must not drop a line for a
            // packed-based remaining figure. LineQuantity == LineQuantityPacked +
            // LineQuantityBackOrdered is false on partially fulfilled orders and
            // used to remove valid scans from the payload. Forward the loaded rows
            // (scanned and zero-quantity) unchanged; the server replaces stale
            // zeros and adds fillers for every open line it did not receive.
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
                LineQuantityBackOrdered = x.LineQuantityBackOrdered,

                IsClosed = x.IsClosed,
                QuantityAlreadyShipped = x.QuantityAlreadyShipped,

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

    //async void ToggleWeight()
    //{
    //    ChangeWeight = await GetWeightAsync("", "");
    //}

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

    private bool IsValidForManualEntry(TransferOrderLineVM? row)
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
        MoveOn = false;
        NegateQuantity = false;
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

    private async Task Refresh()
    {
        // Gate is open: nothing to refresh until a vendor barcode is scanned.
        if (VendorId is null) return;

        // Snapshot current scanned state before refreshing
        var scannedState = GoodTOItems
            .Concat(BadTOItems)
            .Concat(DoneTOItems)
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
                    ScanCount = g.First().ScanCount
                });

        // Reload PO items
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOItems);

        // Restore scanned state (including DONE lines, so a line that moved to
        // DONE across the reload keeps its snapshot entry).
        var clampedLines = new List<string>();

        foreach (var item in GoodTOItems.Concat(BadTOItems).Concat(DoneTOItems))
        {
            var key = new
            {
                item.LineSequenceNumber,
                item.NetsuiteMaterialInternalId,
                item.IsBad
            };

            if (scannedState.TryGetValue(key, out var scanned))
            {
                // The plan can shrink across the reload (another picker packed
                // some of it, the order was edited). Restoring the old count
                // would leave the device over the fresh packable quantity, and
                // the scan guard would then allow everything up to the stale
                // total, so clamp and tell the operator what was reduced.
                var clamped = Math.Min(scanned.ScannedQuantity, item.PackableQuantity);
                if (clamped < scanned.ScannedQuantity)
                {
                    clampedLines.Add($"{item.MaterialCode} ({scanned.ScannedQuantity} -> {clamped})");
                }

                item.ScannedQuantity = clamped;
                item.ScannedWeight = scanned.ScannedWeight;
                item.ScanCount = scanned.ScanCount;
            }
        }

        if (clampedLines.Count > 0)
        {
            await Toast.Warning($"Refresh reduced the scanned quantity on {clampedLines.Count} line(s) to the current plan: {string.Join(", ", clampedLines)}.");
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        MarkScansClean();
        BroadcastService.BroadcastReceived -= HandleItemScan;

        await DisposeLastScannedPanel();
    }

    #region Button States
    private ToggleState ScanState { get; set; } = ToggleState.Base;

    private bool NextScanIsBad => ScanState == ToggleState.Bad;
    #endregion
}
