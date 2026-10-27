using Mobile.MAUI.Services;
using Microsoft.AspNetCore.Components.Web;
using Shared.Libraries.ViewModel.Returns;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.Packing.Returns;

public partial class ReturnsView : IDisposable
{
    private string SearchText { get; set; } = string.Empty;
    private string ErrorMessage { get; set; } = string.Empty;
    private int _lookupInProgress;
    private bool IsLoading => Volatile.Read(ref _lookupInProgress) != 0;
    protected override Task OnInitializedAsync()
    {
        BroadcastService.BroadcastReceived += HandleItemScan;
        return Task.CompletedTask;
    }
    private Task OnSearchInput(ChangeEventArgs args) { SearchText = args.Value?.ToString() ?? string.Empty; return Task.CompletedTask; }
    private async Task HandleEnter(KeyboardEventArgs args) { if (args.Key == "Enter") await SubmitLookup(); }
    private async void HandleItemScan(object? sender, string message) { if (!string.IsNullOrWhiteSpace(message)) { SearchText = message.Trim(); try { await InvokeAsync(SubmitLookup); } catch { } } }
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
