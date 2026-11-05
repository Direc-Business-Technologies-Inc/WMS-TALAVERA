using Microsoft.AspNetCore.Components.Web;
using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.PurchaseOrder;
using Shared.Libraries.ViewModel.Authentication;
using System.Text.Json;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderView : IDisposable
{
    private string SearchText { get; set; } = string.Empty;
    private int _lookupInProgress;
    private bool IsLoading => Volatile.Read(ref _lookupInProgress) != 0;
    private string ErrorMessage { get; set; } = string.Empty;
    private List<PurchaseOrderVM> Data { get; set; } = [];
    private List<PurchaseOrderVM> FilteredData { get; set; } = [];
    private bool IsListLoading { get; set; }
    private string ListErrorMessage { get; set; } = string.Empty;

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
            ApplySearch();
            try { await InvokeAsync(SubmitLookup); }
            catch { /* Routed component actions normally absorb request errors; never leak scanner callback exceptions. */ }
        }
    }

    private Task OnSearchInput(ChangeEventArgs args) { SearchText = args.Value?.ToString() ?? string.Empty; ApplySearch(); return Task.CompletedTask; }
    private void ApplySearch() => FilteredData = string.IsNullOrWhiteSpace(SearchText) ? Data.ToList() : Data.Where(x => new[] { x.OrderNumber, x.CustomerName, x.OrderStatus, x.OrderType }.Any(v => v?.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) == true) || x.NetsuiteOrderDocumentDate.ToString("MMMM d, yyyy").Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    private Task ClearSearch() { SearchText = string.Empty; ApplySearch(); return Task.CompletedTask; }
    private async Task LoadData()
    {
        if (IsListLoading) return;
        IsListLoading = true; ListErrorMessage = string.Empty; await InvokeAsync(StateHasChanged);
        try
        {
            var userAuth = await SecureStorage.GetAsync("UserAuth");
            var auth = userAuth is null ? null : JsonSerializer.Deserialize<AuthenticationVM>(userAuth);
            if (auth is null) { ListErrorMessage = "Unable to read your user information. Sign in again and retry."; return; }
            var result = await Client.Post<List<PurchaseOrderVM>>("/Receiving/PurchaseOrder/PendingReceipt", new { NetsuiteUserSubsidiaryInternalId = auth.NetsuiteSubsidiaryInternalId, NetsuiteUserInternalId = auth.NetsuiteEmployeeInternalId });
            if (result is null || !result.Success) { ListErrorMessage = result?.ErrorMessage ?? "Unable to load pending purchase orders. Try again."; return; }
            Data = result.Data ?? []; ApplySearch();
        }
        catch (Exception ex) when (ex is not NotAuthorizedException) { ListErrorMessage = "Unable to load pending purchase orders. Check your connection and try again."; }
        finally { IsListLoading = false; await InvokeAsync(StateHasChanged); }
    }
    private async Task HandleEnter(KeyboardEventArgs args) { if (args.Key == "Enter") await SubmitLookup(); }

    #endregion

    public void Dispose() => BroadcastService.BroadcastReceived -= HandleItemScan;
}
