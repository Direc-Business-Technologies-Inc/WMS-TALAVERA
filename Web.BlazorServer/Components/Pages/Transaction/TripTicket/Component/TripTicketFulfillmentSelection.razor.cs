using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Implementations.Transaction.TripTicket;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Others;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component;

public partial class TripTicketFulfillmentSelection
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ITripTicketHandler TripTicketHandler { get; set; } = default!;

    [Parameter] public required TripTicketVM Document { get; set; }
    [Parameter] public EventCallback<TripTicketVM> DocumentChanged { get; set; }
    //[Parameter] public IEnumerable<ItemFulfillmentVM> Fulfillments { get; set; } = [];

    List<AppFilterDescriptor> Filters { get; set; } = [];
    AppDataGrid<ItemFulfillmentVM> FulfillmentsDataGrid { get; set; } = default!;
    DataGridSettings FulfillmentsDataGridSettings { get; set; } = new();
    private string? SearchText { get; set; }

    string ActionGetFulfillments { get; } = EnumHelper.GetEnumDescription(AppActions.GetPackedTripTicketFulfillments);

    IList<ItemFulfillmentVM> SelectedFulfillments { get; set; } = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!GridSettingsLoaded) await LoadGridSettings();
        }
    }

    async Task LoadGridSettings()
    {
        SelectedFulfillments = [.. Document.ItemFulfillments];

        await GridSettingsService.SetGridSettings(FulfillmentsDataGrid.DataGrid, settings => FulfillmentsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;
        await FulfillmentsDataGrid.DataGrid.ReloadSettings();
        await FulfillmentsDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<ItemFulfillmentVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetFulfillments, true);

            var filters = new List<AppFilterDescriptor>();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchFilter = new AppFilterDescriptor
                {
                    LogicalOperator = LogicalOperatorEnum.OR,
                    Filters =
                    [
                        new AppFilterDescriptor
                        {
                            Property = nameof(ItemFulfillmentVM.OrderNumber),
                            Value = SearchText,
                            FilterValueType = FilterValueTypeEnum.String,
                            ComparisonOperator = ComparisonOperatorEnum.Contains
                        }
                    ]
                };

                filters.Add(searchFilter);
            }

            if (Filters.Count > 0)
            {
                filters.AddRange(Filters);
            }

            intent.Filters = filters;

            var response = await TripTicketHandler.GetPackedItemFulfillmentsAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetFulfillments));

        AppBusyService.SetBusy(ActionGetFulfillments, false);

        return DataGridResultVM<ItemFulfillmentVM>.New(
            action.Result.Data ?? [],
            action.Result.Count);
    }

    async Task OnSearchChanged(object? value)
    {
        SearchText = value?.ToString();

        await FulfillmentsDataGrid.ReloadDataAsync();
    }

    async Task ClearSearch()
    {
        SearchText = null;

        await FulfillmentsDataGrid.ReloadDataAsync();
    }

    async Task SelectItem(ItemFulfillmentVM item)
    {
        var selectedItem = SelectedFulfillments.FirstOrDefault(x => x.OrderNumber == item.OrderNumber);

        if (selectedItem is not null)
        {
            SelectedFulfillments.Remove(selectedItem);

            if (Document.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId == item.NetsuiteOrderInternalId))
                Document.ItemFulfillments = [.. Document.ItemFulfillments.Where(x => x.NetsuiteOrderInternalId != item.NetsuiteOrderInternalId)];
        }
        else
        {
            SelectedFulfillments.Add(item);

            if (!Document.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId == item.NetsuiteOrderInternalId))
                Document.ItemFulfillments = [.. Document.ItemFulfillments, item];
        }

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(FulfillmentsDataGrid.DataGrid);
    }
}
