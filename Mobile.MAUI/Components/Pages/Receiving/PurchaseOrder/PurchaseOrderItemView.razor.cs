using Microsoft.JSInterop;
using Shared.Libraries.ViewModel;
using Mobile.MAUI.Services;

using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.MauiProgram;
using Mobile.MAUI.Helpers.Extensions;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderItemView
{
    [Parameter]
    public string OrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    AppAction<List<PurchaseOrderLineVM>> ActionGetPOItems { get; set; }
    AppAction ActionUpdateStartTime { get; set; }
    AppAction ActionSaveScan { get; set; }

    List<PurchaseOrderLineVM> POItems = [];
    PurchaseOrderLineVM? LastScanned => POItems.OrderByDescending(x => x.ScanCount).FirstOrDefault();

    int ScanCount { get; set; }
    bool SaveBtnDisabled => ScanCount == 0;

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
                POItems = result.Data ?? [];

                await InvokeAsync(StateHasChanged);
            },
        };

        ActionSaveScan = new AppAction
        {
            Name = "SavePicklistScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post("/picking/save-scan", new { POItems = POItems });
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
        }

        if (POItems.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/PicklistIntersectionObserver.js");

            await JsObj.InvokeVoidAsync("ObserveRecentScanned");
        }
    }

    async void HandleItemScan(object sender, string message)
    {
        try
        {

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    // Toggle per-item quality classification between Good (IsBad=false) and Bad (IsBad=true)
    void ToggleQuality(PurchaseOrderLineVM item)
    {
        if (item is null) return;

        item.IsBad = !item.IsBad;

        // UI update. If you need to persist the classification immediately, call an API here.
        InvokeAsync(StateHasChanged);
    }
}