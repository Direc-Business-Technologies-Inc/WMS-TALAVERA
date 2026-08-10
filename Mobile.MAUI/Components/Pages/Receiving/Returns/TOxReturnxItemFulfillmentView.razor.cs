using Microsoft.JSInterop;
using Shared.Libraries.ViewModel.ItemFulfillment;
using static Mobile.MAUI.Enums.CustomEnum;
using static Mobile.MAUI.MauiProgram;
using AppAction = Mobile.MAUI.Services.AppAction;
using static Mobile.MAUI.Helpers.FormatHelper;
using Mobile.MAUI.Services;
using System.Globalization;

namespace Mobile.MAUI.Components.Pages.Receiving.Returns;

public partial class TOxReturnxItemFulfillmentView : IAsyncDisposable
{
    [Parameter]
    public int NetsuiteOrderInternalId { get; set; }

    [Parameter]
    public string TOOrderNumber { get; set; }

    private IJSObjectReference JsObj { get; set; }

    List<ItemFulfillmentVM> Data { get; set; } = [];

    AppAction<List<ItemFulfillmentVM>> ActionGetTOxItemfulfillments;

    private List<ItemFulfillmentVM> FilteredData { get; set; } = [];
    private string SearchText { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        ActionGetTOxItemfulfillments = new AppAction<List<ItemFulfillmentVM>>
        {
            Name = "GetTOxItemfulfillments",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Post<List<ItemFulfillmentVM>>("/Receiving/Returns/ItemFulfillment", new { NetsuiteOrderInternalId = NetsuiteOrderInternalId });
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOxItemfulfillments);
    }

    async Task LoadItemFulfillments()
    {
        await ActionFactory.ExecuteAppActionAsync(ActionGetTOxItemfulfillments);
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

    private static bool MatchesSearch(ItemFulfillmentVM row, string search)
    {
        if (ContainsIgnoreCase(row.OrderNumber, search))
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