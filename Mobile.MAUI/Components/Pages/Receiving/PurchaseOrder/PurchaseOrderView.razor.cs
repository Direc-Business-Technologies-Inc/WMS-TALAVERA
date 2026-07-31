using Microsoft.JSInterop;
using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.PurchaseOrder;
using System.Globalization;
using System.Text.Json;
using static Mobile.MAUI.MauiProgram;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder;

public partial class PurchaseOrderView : IAsyncDisposable
{
    private IJSObjectReference JsObj { get; set; }

    List<PurchaseOrderVM> Data { get; set; } = [];

    AppAction<List<PurchaseOrderVM>> ActionGetPurchaseOrder;

    int UserSubsidiaryId { get; set; }
    int UserId { get; set; }

    private List<PurchaseOrderVM> FilteredData { get; set; } = [];
    private string SearchText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        ActionGetPurchaseOrder = new AppAction<List<PurchaseOrderVM>>
        {
            Name = "GetPurchaseOrder",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<PurchaseOrderVM>>("/Receiving/PurchaseOrder/PendingReceipt", new { NetsuiteUserSubsidiaryInternalId = UserSubsidiaryId, NetsuiteUserInternalId = UserId });
                return res;
            },
            OnSuccess = async (result) =>
            {
                Data = result.Data ?? [];

                ApplySearch();
                await InvokeAsync(StateHasChanged);
            }
        };

        BroadcastService.BroadcastReceived += HandleItemScan;
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
                UserId = auth.NetsuiteEmployeeInternalId;
            }

            await LoadData();
        }

        if (FilteredData.Count > 0 && JsObj is null)
        {
            JsObj = await Js.InvokeAsync<IJSObjectReference>("import", "./js/IntersectionObserver.js");
            await JsObj.InvokeVoidAsync("Observe");
        }
    }

    async Task LoadData()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetPurchaseOrder);
    }

    #region Search
    async void HandleItemScan(object sender, string message)
    {
        SearchText = message?.Trim();

        ApplySearch();

        await InvokeAsync(StateHasChanged);
    }

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
            FilteredData = Data.ToList();
            return;
        }

        var search = SearchText.Trim();

        FilteredData = Data
            .Where(row => MatchesSearch(row, search))
            .ToList();
    }

    private static bool MatchesSearch(PurchaseOrderVM row, string search)
    {
        if (ContainsIgnoreCase(row.OrderNumber, search))
            return true;

        if (ContainsIgnoreCase(row.CustomerName, search))
            return true;

        if (ContainsIgnoreCase(row.OrderStatus, search))
            return true;

        if (ContainsIgnoreCase(row.OrderType, search))
            return true;

        var createdDateFormats = new[]
        {
            row.NetsuiteOrderCreatedDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            row.NetsuiteOrderCreatedDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            row.NetsuiteOrderCreatedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            row.NetsuiteOrderCreatedDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
            row.NetsuiteOrderCreatedDate.ToString("M/d/yyyy", CultureInfo.InvariantCulture)
        };

        if (createdDateFormats.Any(date => ContainsIgnoreCase(date, search)))
            return true;

        if (DateTime.TryParse(search, out var parsedDate))
        {
            return row.NetsuiteOrderCreatedDate.Date == parsedDate.Date;
        }

        return false;
    }

    private static bool ContainsIgnoreCase(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

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