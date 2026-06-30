using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.InventoryCounting;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.InventoryCounting.InventoryWorksheet;

public partial class CreateInventoryWorksheetView
{
    private IJSObjectReference JsObj { get; set; }

    List<InventoryItemVM> Data { get; set; } = [];
    List<LocationVM> Locations { get; set; } = [];

    AppAction<List<InventoryItemVM>> ActionGetInventoryItems;

    AppAction<List<LocationVM>> ActionGetLocations;

    AppAction<List<ItemBarcodesPerUoMVM>> ActionGetItemBarcodes { get; set; }

    AppAction<bool> ActionSaveScan { get; set; }


    List<InventoryWorksheetLineVM> ICItems = [];

    List<ItemBarcodesPerUoMVM> ItemBarcodes = [];
    List<BarcodeRequestVM> ItemRequest = [];

    int ScanCount { get; set; }
    int ActiveTabIndex { get; set; } = 0;
    int SelectedLocationInternalId { get; set; }
    bool SaveBtnDisabled => ICItems.Count == 0;
    bool MoveOn = false;

    bool NextScanIsBad = false;

    protected override async Task OnInitializedAsync()
    {
        ActionGetLocations = new AppAction<List<LocationVM>>
        {
            Name = "GetLocations",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<LocationVM>>("/Lookup/Locations");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Locations = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetInventoryItems = new AppAction<List<InventoryItemVM>>
        {
            Name = "GetItemFulfillments",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<InventoryItemVM>>("/InventoryCounting/Worksheet/Items");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Data = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
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
            Name = "SaveWorksheetScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/InventoryCounting/Worksheet/SaveScan", new {InventoryCountItems = ICItems, Location = SelectedLocationInternalId});
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
                NavManager.NavigateTo("/");
            }
        };

        BroadcastService.BroadcastReceived += HandleScan;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ActionFactory.ExecuteAppActionAsync(ActionGetLocations);
            await ActionFactory.ExecuteAppActionAsync(ActionGetInventoryItems);

            ItemRequest = Data.Select(i => new BarcodeRequestVM
            {
                NetsuiteMaterialInternalId = i.NetsuiteMaterialInternalId,
            }).ToList();

            await ActionFactory.ExecuteAppActionAsync(ActionGetItemBarcodes);
        }

        if (Data.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }


    async void HandleScan(object sender, string message)
    {
        try
        {
            if (ScanState == ToggleState.Base && !MoveOn && !NegateQuantity) return;

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

            var isScanned = ICItems.Exists(x => x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId);

            if (isScanned)
            {
                if (MoveOn)
                {
                    await MoveScan(barcode);
                    return;
                }

                if (NegateQuantity)
                {
                    await NegateScannedItem(barcode);
                    return;
                }
            }
            else if(!MoveOn && !NegateQuantity)
            {
                ICItems.Add(new InventoryWorksheetLineVM
                {
                    NetsuiteMaterialInternalId = barcode.NetsuiteMaterialInternalId,
                    MaterialCode = barcode.MaterialCode,
                    MaterialName = barcode.MaterialName,
                    MaterialWeight = barcode.MaterialWeight
                });
            }
            else
            {
                await Toast.Warning("No scanned quantity to move or negate for this item.");
                return;
            }

            InventoryWorksheetLineVM? line = ICItems.FirstOrDefault(x =>
                    x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var badlineTotal = line.BadScannedQuantity;
            var goodLineTotal = line.GoodScannedQuantity;

            var scannedQuantity = barcode.UoMRate;

            if (NextScanIsBad)
            {
                line.BadScannedQuantity += scannedQuantity;

                line.ScanCount++;
            }
            else
            {
                line.GoodScannedQuantity += scannedQuantity;

                line.ScanCount++;
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
        await InvokeAsync(StateHasChanged);
    }

    async Task NegateScannedItem(ItemBarcodesPerUoMVM barcode)
    {
        try
        {
            InventoryWorksheetLineVM? line = ICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var badlineTotal = line.BadScannedQuantity;
            var goodLineTotal = line.GoodScannedQuantity;

            var scannedQuantity = barcode.UoMRate;

            if (ActiveTabIndex == 1)
            {
                if (badlineTotal == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                if (badlineTotal < scannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                line.BadScannedQuantity -= scannedQuantity;

                line.ScanCount++;
            }
            else
            {
                if (goodLineTotal == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                if (goodLineTotal < scannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                line.GoodScannedQuantity -= scannedQuantity;

                line.ScanCount++;
            }

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

        await InvokeAsync(StateHasChanged);
    }

    async Task MoveScan(ItemBarcodesPerUoMVM barcode)
    {
        try
        {
            InventoryWorksheetLineVM? line = ICItems.FirstOrDefault(x =>
                x.NetsuiteMaterialInternalId == barcode.NetsuiteMaterialInternalId);

            if (line is null)
            {
                await Toast.Warning("Item not found in this Inventory Count.");
                return;
            }

            var badlineTotal = line.BadScannedQuantity;
            var goodLineTotal = line.GoodScannedQuantity;

            var scannedQuantity = barcode.UoMRate;

            if (ActiveTabIndex == 1)
            {
                if (badlineTotal == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                if (badlineTotal < scannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                line.BadScannedQuantity -= scannedQuantity;

                line.GoodScannedQuantity += scannedQuantity;

                line.ScanCount++;
            }
            else
            {
                if (goodLineTotal == 0)
                {
                    await Toast.Warning("No scanned quantity to move for this item.");
                    return;
                }

                if (goodLineTotal < scannedQuantity)
                {
                    await Toast.Warning("Not enough scanned quantity to move.");
                    return;
                }

                line.GoodScannedQuantity -= scannedQuantity;

                line.BadScannedQuantity += scannedQuantity;

                line.ScanCount++;
            }

            ScanCount++;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }

        await InvokeAsync(StateHasChanged);
    }

    async Task OnLocationChanged(object value)
    {

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
    }

    public async ValueTask DisposeAsync()
    {
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

    private string ScanStateIcon => ScanState switch
    {
        ToggleState.Base => "check",
        ToggleState.Good => "check",
        ToggleState.Bad => "block",
        _ => "check"
    };

    private string ScanStateLabel => ScanState switch
    {
        ToggleState.Base => "Good",
        ToggleState.Good => "Good",
        ToggleState.Bad => "Bad",
        _ => "Good"
    };

    private ButtonStyle ScanStateButtonStyle => ScanState switch
    {
        ToggleState.Base => ButtonStyle.Base,
        ToggleState.Good => ButtonStyle.Success,
        ToggleState.Bad => ButtonStyle.Danger,
        _ => ButtonStyle.Base
    };

    private void ToggleScanState()
    {
        ScanState = ScanState switch
        {
            ToggleState.Base => ToggleState.Good,
            ToggleState.Good => ToggleState.Bad,
            ToggleState.Bad => ToggleState.Base,
            _ => ToggleState.Base
        };

        NextScanIsBad = ScanState switch
        {
            ToggleState.Base => false,
            ToggleState.Good => false,
            ToggleState.Bad => true,
            _ => false
        };

        InvokeAsync(StateHasChanged);
    }
    #endregion
}