using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.InventoryCounting;
using System.Text.Json;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.InventoryCounting.InventoryWorksheet;

public partial class CreateInventoryWorksheetView : IAsyncDisposable
{
    private IJSObjectReference? JsObj { get; set; }

    private List<InventoryItemVM> Data { get; set; } = [];
    private List<LocationVM> Locations { get; set; } = [];
    private List<BinVM> Bins { get; set; } = [];

    private AppAction<List<InventoryItemVM>>? ActionGetInventoryItems { get; set; }
    private AppAction<List<LocationVM>>? ActionGetLocations { get; set; }
    private AppAction<List<BinVM>>? ActionGetBins { get; set; }
    private AppAction<InventoryItemVM>? ActionGetItemPerBarcodes { get; set; }
    private AppAction<bool>? ActionSaveScan { get; set; }

    private List<InventoryWorksheetLineVM> ICItems { get; set; } = [];

    private InventoryItemVM ItemScanned { get; set; }
    private InventoryItemVM? ManualItem { get; set; }
    private decimal ManualQuantity { get; set; }
    private bool ShowManualEntry { get; set; }
    //private List<BarcodeRequestVM> ItemRequest { get; set; } = [];
    private string BarcodeRequest { get; set; }

    private int _scanCount;
    private int ScanCount
    {
        get => _scanCount;
        set
        {
            _scanCount = value;
            if (value > 0) MarkScansDirty();
        }
    }
    private int ActiveTabIndex { get; set; } = 0;
    private int SelectedLocationInternalId { get; set; }
    private int SelectedBinInternalId { get; set; }
    private int UserSubsidiaryId { get; set; }

    private bool MoveOn { get; set; }
    private bool NegateQuantity { get; set; }
    private bool IsActionPanelCollapsed { get; set; }
    private bool ShowAllScannedItems { get; set; }

    private bool SaveBtnDisabled => ICItems.Count == 0;

    private bool HasSelectedLocation => SelectedLocationInternalId != 0;

    private bool HasBins => Bins.Count != 0;

    private bool RequiresBinSelection => HasSelectedLocation && HasBins;

    private int CurrentBinInternalId => HasBins ? SelectedBinInternalId : 0;

    private IEnumerable<InventoryWorksheetLineVM> GoodScannedItems =>
        ICItems.Where(x =>
            x.GoodScannedQuantity != 0 &&
            x.NetsuiteBinInternalId == CurrentBinInternalId);

    private IEnumerable<InventoryWorksheetLineVM> BadScannedItems =>
        ICItems.Where(x =>
            x.BadScannedQuantity != 0 &&
            x.NetsuiteBinInternalId == CurrentBinInternalId);

    private IEnumerable<ScannedItemSummaryVM> AllScannedItems =>
    ICItems
        .Where(x => x.GoodScannedQuantity != 0 || x.BadScannedQuantity != 0)
        .GroupBy(x => x.NetsuiteMaterialInternalId)
        .Select(group => new ScannedItemSummaryVM
        {
            NetsuiteMaterialInternalId = group.Key,
            MaterialCode = group.FirstOrDefault()?.MaterialCode,
            MaterialName = group.FirstOrDefault()?.MaterialName,
            UoMName = group.FirstOrDefault()?.UoMName,
            GoodScannedQuantity = group.Sum(x => x.GoodScannedQuantity),
            BadScannedQuantity = group.Sum(x => x.BadScannedQuantity)
        })
        .OrderBy(x => x.MaterialName)
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        ActionGetLocations = new AppAction<List<LocationVM>>
        {
            Name = "GetLocations",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<List<LocationVM>>("/Lookup/Susidiary/Locations", new { NetsuiteUserSubsidiaryInternalId = UserSubsidiaryId });

                return res;
            },
            OnSuccess = async (result) =>
            {
                Locations = result.Data ?? [];
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetBins = new AppAction<List<BinVM>>
        {
            Name = "GetBinsPerLocation",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<List<BinVM>>(
                    "/Lookup/BinLocations",
                    new
                    {
                        Location = SelectedLocationInternalId
                    });

                return res;
            },
            OnSuccess = async (result) =>
            {
                Bins = result.Data ?? [];
                await InvokeAsync(StateHasChanged);
            }
        };

        //ActionGetInventoryItems = new AppAction<List<InventoryItemVM>>
        //{
        //    Name = "GetInventoryWorksheetItems",
        //    TaskAsync = async () =>
        //    {
        //        await InvokeAsync(StateHasChanged);

        //        var res = await Client.Get<List<InventoryItemVM>>("/InventoryCounting/Worksheet/Items");

        //        return res;
        //    },
        //    OnSuccess = async (result) =>
        //    {
        //        Data = result.Data ?? [];
        //        await InvokeAsync(StateHasChanged);
        //    }
        //};

        ActionGetInventoryItems = new AppAction<List<InventoryItemVM>>
        {
            Name = "GetInventoryWorksheetItems",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Get<List<InventoryItemVM>>("/InventoryCounting/Worksheet/Items");

                return res;
            },
            OnSuccess = async (result) =>
            {
                Data = result.Data ?? [];
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetItemPerBarcodes = new AppAction<InventoryItemVM>
        {
            Name = "GetItemBarcodes",

            TaskAsync = async () =>
            {
                var result = await Client.Post<InventoryItemVM>(
                    "/Item/PerBarcode",
                    new { Barcode = BarcodeRequest }
                );

                return result;
            },

            OnSuccess = async result =>
            {
                if (result?.Data is not null)
                {
                    ItemScanned = result.Data;
                }

                await InvokeAsync(StateHasChanged);
            }
        };


        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveWorksheetScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);

                var res = await Client.Post<bool>(
                    "/InventoryCounting/Worksheet/SaveScan",
                    new
                    {
                        InventoryCountItems = ICItems,
                        Location = SelectedLocationInternalId,
                        NetsuiteUserSubsidiaryInternalId = UserSubsidiaryId
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
                await Toast.Success("Scanned items saved successfully");
                NavManager.NavigateTo("/");
            }
        };

        BroadcastService.BroadcastReceived += HandleScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string? userAuth = await SecureStorage.GetAsync("UserAuth");
            if (userAuth is not null)
            {
                var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);

                UserSubsidiaryId = auth.NetsuiteSubsidiaryInternalId;
            }


            if (ActionGetLocations is not null)
            {
                await ActionFactory.ExecuteAppActionAsync(ActionGetLocations);
            }

            //if (ActionGetInventoryItems is not null)
            //{
            //    await ActionFactory.ExecuteAppActionAsync(ActionGetInventoryItems);
            //}

            if (ActionGetInventoryItems is not null)
            {
                await ActionFactory.ExecuteAppActionAsync(ActionGetInventoryItems);
            }

            //ItemRequest = Data
            //    .Select(i => new BarcodeRequestVM
            //    {
            //        NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId
            //    })
            //    .ToList();

            //if (ActionGetItemBarcodes is not null)
            //{
            //    await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
            //}
        }

        if (Data.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/IntersectionObserver.js");

            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    private readonly SemaphoreSlim _scanLock = new(1, 1);

    //private async void HandleScan(object sender, string message)
    //{
    //    await _scanLock.WaitAsync();
    //    try
    //    {
    //        if (SelectedLocationInternalId == 0)
    //        {
    //            await Toast.Warning("Please select a location first.");
    //            return;
    //        }

    //        if (RequiresBinSelection && SelectedBinInternalId == 0)
    //        {
    //            await Toast.Warning("Please select a bin first.");
    //            return;
    //        }

    //        if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity)
    //            return;

    //        var scanned = message?.Trim();

    //        if (string.IsNullOrWhiteSpace(scanned))
    //            return;

    //        var result = await Client.Post<InventoryItemVM>(
    //                "/Item/PerBarcode",
    //                new { Barcode = scanned }
    //            );

    //        var barcode = result.Data;

    //        if (barcode is null)
    //        {
    //            await Toast.Warning($"Unknown barcode: {scanned}");
    //            return;
    //        }

    //        var binInternalId = CurrentBinInternalId;

    //        var isScanned = ICItems.Exists(x =>
    //            x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
    //            x.NetsuiteBinInternalId == binInternalId);

    //        if (isScanned)
    //        {
    //            if (MoveOn)
    //            {
    //                await MoveScan(barcode);
    //                return;
    //            }

    //            if (NegateQuantity)
    //            {
    //                await NegateScannedItem(barcode);
    //                return;
    //            }
    //        }
    //        else if (!MoveOn && !NegateQuantity)
    //        {
    //            ICItems.Add(new InventoryWorksheetLineVM
    //            {
    //                NetsuiteMaterialInternalId = barcode.NetsuiteMaterialInternalId,
    //                MaterialCode = barcode.MaterialCode,
    //                MaterialName = barcode.MaterialName,
    //                MaterialWeight = barcode.MaterialWeight,
    //                NetsuiteBinInternalId = binInternalId
    //            });
    //        }
    //        else
    //        {
    //            await Toast.Warning("No scanned quantity to move or remove for this item.");
    //            return;
    //        }

    //        var line = ICItems.FirstOrDefault(x =>
    //            x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
    //            x.NetsuiteBinInternalId == binInternalId);

    //        if (line is null)
    //        {
    //            await Toast.Warning("Item not found in this Inventory Count.");
    //            return;
    //        }

    //        var scannedQuantity = barcode.UoMRate;

    //        if (NextScanIsBad)
    //        {
    //            line.BadScannedQuantity += scannedQuantity;
    //        }
    //        else
    //        {
    //            line.GoodScannedQuantity += scannedQuantity;
    //        }

    //        line.ScanCount++;
    //        ScanCount++;

    //        await InvokeAsync(StateHasChanged);
    //    }
    //    catch (Exception e)
    //    {
    //        await Toast.Error(e.Message);
    //    }
    //    finally
    //    {
    //        _scanLock.Release();
    //    }
    //}

    private async void HandleScan(object sender, string message)
    {
        await _scanLock.WaitAsync();

        try
        {
            // Validation

            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

            // API - doesn't modify UI state
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

            // UI/state modification
            await InvokeAsync(async () =>
            {
                await ProcessScannedItem(barcode);
                await InvokeAsync(StateHasChanged);
            });
        }
        catch (NotAuthorizedException)
        {
            await Toast.Error("Session expired. Please sign in again.");
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private async Task ProcessScannedItem(InventoryItemVM barcode)
    {
        var binInternalId = CurrentBinInternalId;

        if (barcode.UoMRate <= 0)
        {
            await Toast.Warning($"{barcode.MaterialCode} has no UoM conversion rate. Scan not recorded.");
            return;
        }

        var line = ICItems.FirstOrDefault(x =>
            x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
            x.NetsuiteBinInternalId == binInternalId);

        if (line is null)
        {
            if (MoveOn || NegateQuantity)
            {
                await Toast.Warning("Move/Remove mode is on. Turn it off to record scans.");
                return;
            }

            line = new InventoryWorksheetLineVM
            {
                NetsuiteMaterialInternalId = barcode.NetsuiteMaterialInternalId,
                MaterialCode = barcode.MaterialCode,
                MaterialName = barcode.MaterialName,
                MaterialWeight = barcode.MaterialWeight,
                UoMName = barcode.UoMName,
                UoMRate = barcode.UoMRate,
                NetsuiteBinInternalId = binInternalId
            };

            ICItems.Add(line);
        }

        var quantity = barcode.UoMRate;

        if (NextScanIsBad)
            line.BadScannedQuantity += quantity;
        else
            line.GoodScannedQuantity += quantity;

        line.ScanCount++;
        ScanCount++;
    }

    private async Task AddManualItem()
    {
        if (SelectedLocationInternalId == 0)
        {
            await Toast.Warning("Please select a location first.");
            return;
        }

        if (RequiresBinSelection && SelectedBinInternalId == 0)
        {
            await Toast.Warning("Please select a bin first.");
            return;
        }

        if (ManualItem is null || ManualQuantity <= 0)
        {
            await Toast.Warning("Please select an item and enter a quantity.");
            return;
        }

        var binInternalId = CurrentBinInternalId;

        var line = ICItems.FirstOrDefault(x =>
            x.NetsuiteMaterialInternalId == ManualItem.NetsuiteMaterialInternalId &&
            x.NetsuiteBinInternalId == binInternalId);

        if (line is null)
        {
            line = new InventoryWorksheetLineVM
            {
                NetsuiteMaterialInternalId = ManualItem.NetsuiteMaterialInternalId,
                MaterialCode = ManualItem.MaterialCode,
                MaterialName = ManualItem.MaterialName,
                MaterialWeight = ManualItem.MaterialWeight,
                UoMName = ManualItem.UoMName,
                UoMRate = ManualItem.UoMRate,
                NetsuiteBinInternalId = binInternalId
            };

            ICItems.Add(line);
        }

        if (NextScanIsBad)
            line.BadScannedQuantity += ManualQuantity;
        else
            line.GoodScannedQuantity += ManualQuantity;

        line.ScanCount++;

        ManualItem = null;
        ManualQuantity = 0;

        await InvokeAsync(StateHasChanged);
    }

    private void ToggleManualEntry()
    {
        ShowManualEntry = !ShowManualEntry;
        InvokeAsync(StateHasChanged);
    }



    private async Task SaveScan()
    {
        if (SelectedLocationInternalId == 0)
        {
            await Toast.Warning("Please select a location first.");
            return;
        }

        if (RequiresBinSelection && SelectedBinInternalId == 0)
        {
            await Toast.Warning("Please select a bin first.");
            return;
        }

        if (ICItems.Count == 0)
        {
            await Toast.Warning("No scanned items to save.");
            return;
        }

        if (!ICItems.Any(x => x.GoodScannedQuantity > 0 || x.BadScannedQuantity > 0))
        {
            await Toast.Warning("No scanned quantities to save.");
            return;
        }

        if (ICItems.Any(x => x.GoodScannedQuantity < 0 || x.BadScannedQuantity < 0))
        {
            await Toast.Warning("Quantities cannot be negative.");
            return;
        }

        if (ActionSaveScan is not null)
        {
            await ActionFactory.ExecuteAppActionAsync(
                ActionSaveScan,
                confirm: true,
                showToast: true);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task NegateScannedItem(InventoryItemVM barcode)
    {
        try
        {
            var binInternalId = CurrentBinInternalId;

            var line = ICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                x.NetsuiteBinInternalId == binInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var scannedQuantity = barcode.UoMRate;

            if (ActiveTabIndex == 1)
            {
                if (line.BadScannedQuantity == 0)
                {
                    await Toast.Warning("No bad scanned quantity to remove for this item.");
                    return;
                }

                if (line.BadScannedQuantity < scannedQuantity)
                {
                    await Toast.Warning("Not enough bad scanned quantity to remove.");
                    return;
                }

                line.BadScannedQuantity -= scannedQuantity;
            }
            else
            {
                if (line.GoodScannedQuantity == 0)
                {
                    await Toast.Warning("No good scanned quantity to remove for this item.");
                    return;
                }

                if (line.GoodScannedQuantity < scannedQuantity)
                {
                    await Toast.Warning("Not enough good scanned quantity to remove.");
                    return;
                }

                line.GoodScannedQuantity -= scannedQuantity;
            }

            line.ScanCount++;
            ScanCount++;

            if (line.GoodScannedQuantity == 0 && line.BadScannedQuantity == 0)
            {
                ICItems.Remove(line);
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    private async Task MoveScan(InventoryItemVM barcode)
    {
        try
        {
            var binInternalId = CurrentBinInternalId;

            var line = ICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId &&
                x.NetsuiteBinInternalId == binInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var scannedQuantity = barcode.UoMRate;

            if (ActiveTabIndex == 1)
            {
                if (line.BadScannedQuantity == 0)
                {
                    await Toast.Warning("No bad scanned quantity to move for this item.");
                    return;
                }

                if (line.BadScannedQuantity < scannedQuantity)
                {
                    await Toast.Warning("Not enough bad scanned quantity to move.");
                    return;
                }

                line.BadScannedQuantity -= scannedQuantity;
                line.GoodScannedQuantity += scannedQuantity;
            }
            else
            {
                if (line.GoodScannedQuantity == 0)
                {
                    await Toast.Warning("No good scanned quantity to move for this item.");
                    return;
                }

                if (line.GoodScannedQuantity < scannedQuantity)
                {
                    await Toast.Warning("Not enough good scanned quantity to move.");
                    return;
                }

                line.GoodScannedQuantity -= scannedQuantity;
                line.BadScannedQuantity += scannedQuantity;
            }

            line.ScanCount++;
            ScanCount++;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    private async Task OnLocationChanged(object value)
    {
        var previousLocationInternalId = SelectedLocationInternalId;

        if (ICItems.Count > 0)
        {
            var confirmResult = await Dialog.Confirm(
                "Changing the location will clear all scanned and manually entered quantities. Continue?",
                "Change location",
                new ConfirmOptions { OkButtonText = "Yes, change location", CancelButtonText = "Cancel" });

            if (confirmResult != true)
            {
                SelectedLocationInternalId = previousLocationInternalId;
                await InvokeAsync(StateHasChanged);
                return;
            }

            ICItems.Clear();
        }

        SelectedBinInternalId = 0;
        Bins.Clear();

        await InvokeAsync(StateHasChanged);

        if (SelectedLocationInternalId == 0)
            return;

        if (ActionGetBins is not null)
        {
            await ActionFactory.ExecuteAppActionAsync(
                ActionGetBins,
                showToast: false);
        }

        if (Bins.Count == 0)
        {
            SelectedBinInternalId = 0;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnBinChanged(object value)
    {
        await InvokeAsync(StateHasChanged);
    }

    private void ToggleMove()
    {
        MoveOn = !MoveOn;

        if (MoveOn)
        {
            NegateQuantity = false;
        }

        InvokeAsync(StateHasChanged);
    }

    void ToggleScannedListView()
    {
        ShowAllScannedItems = !ShowAllScannedItems;
        InvokeAsync(StateHasChanged);
    }

    private void ToggleActionPanel()
    {
        IsActionPanelCollapsed = !IsActionPanelCollapsed;
    }

    private void ToggleNegateQuantity()
    {
        NegateQuantity = !NegateQuantity;

        if (NegateQuantity)
        {
            MoveOn = false;
        }

        InvokeAsync(StateHasChanged);
    }

    private string GetBinDisplay(int binInternalId)
    {
        if (binInternalId == 0)
            return "No Bin";

        return Bins.FirstOrDefault(x => x.NetsuiteBinInternalId == binInternalId)?.BinNumber
               ?? $"Bin: {binInternalId}";
    }

    public async ValueTask DisposeAsync()
    {
        MarkScansClean();
        BroadcastService.BroadcastReceived -= HandleScan;

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

    private sealed class ScannedItemSummaryVM
    {
        public int NetsuiteMaterialInternalId { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialName { get; set; }
        public string? UoMName { get; set; }
        public decimal GoodScannedQuantity { get; set; }
        public decimal BadScannedQuantity { get; set; }
        public decimal TotalScannedQuantity => GoodScannedQuantity + BadScannedQuantity;
    }
}