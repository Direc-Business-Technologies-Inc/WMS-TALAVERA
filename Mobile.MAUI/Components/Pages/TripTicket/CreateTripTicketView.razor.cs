using Microsoft.JSInterop;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.TripTicket;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;

namespace Mobile.MAUI.Components.Pages.TripTicket;

public partial class CreateTripTicketView : IAsyncDisposable
{

    private IJSObjectReference JsObj { get; set; }

    TripTicketVM TripTicket { get; set; }

    List<ItemFulfillmentVM> Data { get; set; } = [];

    AppAction<List<ItemFulfillmentVM>> ActionGetItemFulfillments;
    AppAction<bool> ActionSaveScan { get; set; }

    List<ItemFulfillmentVM> ScannedItemFulfillments { get; set; } = new();

    int ActiveTabIndex { get; set; } = 0;

    bool MoveOn => ActiveTabIndex == 1;

    protected override async Task OnInitializedAsync()
    {
        ActionGetItemFulfillments = new AppAction<List<ItemFulfillmentVM>>
        {
            Name = "GetItemFulfillments",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<ItemFulfillmentVM>>("/TripTicket/ItemFulfillment/Packed");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Data = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionSaveScan = new AppAction<bool>
        {
            Name = "SaveTripTicketScan",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<bool>("/TripTicket/SaveScan", TripTicket);
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
            await LoadData();
        }

        if (JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadData()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetItemFulfillments);
    }

    async void HandleScan(object sender, string message)
    {
        try
        {
            var scanned = message?.Trim();

            if (string.IsNullOrWhiteSpace(scanned))
                return;

            if (!MoveOn)
            {
                var moveToScanned = Data.FirstOrDefault(x => x.OrderNumber == scanned);

                if (moveToScanned == null)
                {
                    await Toast.Warning($"Unknown Fulfillment Id: {scanned}");
                    return;
                }

                var checkIfAlreadyScanned = ScannedItemFulfillments.Any(x => x.OrderNumber == scanned);

                if (checkIfAlreadyScanned)
                {
                    await Toast.Warning($"{scanned} is already scanned");
                    return;
                }

                await Toast.Success($"Scanned Fulfillment Id: {scanned}");

                moveToScanned.isScanned = true;

                ScannedItemFulfillments.Add(moveToScanned);
            }
            else
            {
                var removeToScanned = ScannedItemFulfillments.FirstOrDefault(x => x.OrderNumber == scanned);

                if (removeToScanned == null)
                {
                    await Toast.Warning($"Unknown Fulfillment Id: {scanned}");
                    return;
                }

                var getIFData = Data.FirstOrDefault(x => x.OrderNumber == scanned);

                await Toast.Success($"Scanned Fulfillment Id: {scanned}");

                getIFData!.isScanned = false;

                ScannedItemFulfillments.Remove(removeToScanned);
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            await Toast.Error(e.Message);
        }
    }

    async Task SaveScan()
    {
        var result = await Dialog.OpenAsync<TripTicketDetailsView>(
            "TripTicket Details",
            new Dictionary<string, object>(),
            new DialogOptions());

        if (result is not TripTicketVM detail)
        {
            return;
        }

        TripTicket = detail;

        if (ScannedItemFulfillments == null || !ScannedItemFulfillments.Any())
        {
            await Toast.Warning("No Item Fulfillments have been scanned.");
            return;
        }

        TripTicket.ItemFulfillments = ScannedItemFulfillments;

        await ActionFactory.ExecuteAppActionAsync(ActionSaveScan, confirm: true, showToast: true);
        await InvokeAsync(StateHasChanged);
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
}