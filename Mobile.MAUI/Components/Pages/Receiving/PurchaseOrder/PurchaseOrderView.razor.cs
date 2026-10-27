using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.PurchaseOrder;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderView : IAsyncDisposable
{
    private IJSObjectReference JsObj { get; set; }

    private string SearchText { get; set; } = string.Empty;
    private int _lookupInProgress;
    private bool IsLoading => Volatile.Read(ref _lookupInProgress) != 0;
    private string ErrorMessage { get; set; } = string.Empty;

    protected override Task OnInitializedAsync()
    {
        BroadcastService.BroadcastReceived += HandleItemScan;
        return Task.CompletedTask;
    }

    private async Task SubmitLookup()
    {
        var orderNumber = SearchText?.Trim();
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            ErrorMessage = "Enter or scan a purchase order number.";
            return;
        }

        if (Interlocked.CompareExchange(ref _lookupInProgress, 1, 0) != 0) return;
        ErrorMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
        try
        {
            var result = await Client.Post<PurchaseOrderVM>("/Receiving/PurchaseOrder/Lookup", new { OrderNumber = orderNumber });
            if (result is null || !result.Success || result.Data is null) { ErrorMessage = result?.ErrorMessage ?? "No eligible purchase order was found."; return; }
            NavManager.NavigateTo($"/receiving/purchaseorder/{Uri.EscapeDataString(result.Data.OrderNumber)}");
        }
        catch (Exception ex) when (ex is not NotAuthorizedException)
        {
            ErrorMessage = ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase)
                ? "No eligible purchase order was found."
                : "Unable to find that purchase order. Check your connection and try again.";
        }
        finally { Interlocked.Exchange(ref _lookupInProgress, 0); await InvokeAsync(StateHasChanged); }
    }

    #region Search
    async void HandleItemScan(object? sender, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            SearchText = message.Trim();
            try { await InvokeAsync(SubmitLookup); }
            catch { /* Routed component actions normally absorb request errors; never leak scanner callback exceptions. */ }
        }
    }

    private Task OnSearchInput(ChangeEventArgs args) { SearchText = args.Value?.ToString() ?? string.Empty; return Task.CompletedTask; }
    private async Task HandleEnter(KeyboardEventArgs args) { if (args.Key == "Enter") await SubmitLookup(); }

    #endregion

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
