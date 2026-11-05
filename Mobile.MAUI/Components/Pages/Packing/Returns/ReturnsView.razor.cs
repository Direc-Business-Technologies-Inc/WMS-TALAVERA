using Mobile.MAUI.Services;
using Microsoft.AspNetCore.Components.Web;
using Shared.Libraries.ViewModel.Returns;
using Shared.Libraries.ViewModel.Authentication;
using System.Text.Json;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.Packing.Returns;

public partial class ReturnsView : IDisposable
{
    private string SearchText { get; set; } = string.Empty;
    private string ErrorMessage { get; set; } = string.Empty;
    private int _lookupInProgress;
    private bool IsLoading => Volatile.Read(ref _lookupInProgress) != 0;
    private List<ReturnsVM> Data { get; set; } = [];
    private List<ReturnsVM> FilteredData { get; set; } = [];
    private bool IsListLoading { get; set; }
    private string ListErrorMessage { get; set; } = string.Empty;
    protected override Task OnInitializedAsync()
    {
        BroadcastService.BroadcastReceived += HandleItemScan;
        return Task.CompletedTask;
    }
    private Task OnSearchInput(ChangeEventArgs args) { SearchText = args.Value?.ToString() ?? string.Empty; ApplySearch(); return Task.CompletedTask; }
    private void ApplySearch() => FilteredData = string.IsNullOrWhiteSpace(SearchText) ? Data.ToList() : Data.Where(x => new[] { x.OrderNumber, x.OrderStatus, x.OrderType }.Any(v => v?.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) == true) || x.NetsuiteOrderCreatedDate.ToString("MMMM d, yyyy").Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    private Task ClearSearch() { SearchText = string.Empty; ApplySearch(); return Task.CompletedTask; }
    private async Task LoadData()
    {
        if (IsListLoading) return; IsListLoading = true; ListErrorMessage = string.Empty; await InvokeAsync(StateHasChanged);
        try
        {
            var userAuth = await SecureStorage.GetAsync("UserAuth"); var auth = userAuth is null ? null : JsonSerializer.Deserialize<AuthenticationVM>(userAuth);
            if (auth is null) { ListErrorMessage = "Unable to read your user information. Sign in again and retry."; return; }
            var result = await Client.Post<List<ReturnsVM>>("/Packing/Returns/PendingFulfillment", new { NetsuiteUserSubsidiaryInternalId = auth.NetsuiteSubsidiaryInternalId, NetsuiteUserInternalId = auth.NetsuiteEmployeeInternalId });
            if (result is null || !result.Success) { ListErrorMessage = result?.ErrorMessage ?? "Unable to load pending returns. Try again."; return; }
            Data = result.Data ?? []; ApplySearch();
        }
        catch (Exception ex) when (ex is not NotAuthorizedException) { ListErrorMessage = "Unable to load pending returns. Check your connection and try again."; }
        finally { IsListLoading = false; await InvokeAsync(StateHasChanged); }
    }
    private async Task HandleEnter(KeyboardEventArgs args) { if (args.Key == "Enter") await SubmitLookup(); }
    private async void HandleItemScan(object? sender, string message) { if (!string.IsNullOrWhiteSpace(message)) { SearchText = message.Trim(); ApplySearch(); try { await InvokeAsync(SubmitLookup); } catch { } } }
    private async Task SubmitLookup()
    {
        var number = SearchText.Trim();
        if (number.Length == 0) { ErrorMessage = "Enter or scan a return number."; return; }
        if (Interlocked.CompareExchange(ref _lookupInProgress, 1, 0) != 0) return;
        ErrorMessage = string.Empty; await InvokeAsync(StateHasChanged);
        try
        {
            var result = await Client.Post<ReturnsVM>("/Packing/Returns/Lookup", new { OrderNumber = number });
            if (result is null || !result.Success || result.Data is null) { ErrorMessage = result?.ErrorMessage ?? "No eligible return was found."; return; }
            NavManager.NavigateTo($"/packing/returns/{Uri.EscapeDataString(result.Data.OrderNumber)}");
        }
        catch (Exception ex) when (ex is not NotAuthorizedException) { ErrorMessage = ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase) ? "No eligible return was found." : "Unable to find that return. Check your connection and try again."; }
        finally { Interlocked.Exchange(ref _lookupInProgress, 0); await InvokeAsync(StateHasChanged); }
    }
    public void Dispose() => BroadcastService.BroadcastReceived -= HandleItemScan;
}
