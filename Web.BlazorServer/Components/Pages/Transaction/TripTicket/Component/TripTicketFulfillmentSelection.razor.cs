using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component;

public partial class TripTicketFulfillmentSelection
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ITripTicketHandler TripTicketHandler { get; set; } = default!;

    [Parameter] public required TripTicketVM Document { get; set; }
    [Parameter] public EventCallback<TripTicketVM> DocumentChanged { get; set; }

    [Parameter] public List<LocationVM> DestinationLocations { get; set; } = new();
    [Parameter] public List<SubsidiaryVM> ToSubsidiaries { get; set; } = new();
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


            bool hasOtherItemsWithSameLocation = SelectedFulfillments
            .Any(x => x.NetsuiteToLocationInternalId == item.NetsuiteToLocationInternalId);

            bool hasOtherItemsWithSameSubsidiary = SelectedFulfillments
            .Any(x => x.NetsuiteToSubsidiaryInternalId == item.NetsuiteToSubsidiaryInternalId);

            if (!hasOtherItemsWithSameLocation && Document.Destinations is not null)
            {
                Document.Destinations = [.. Document.Destinations
                .Where(d => d.NetsuiteLocationInternalId != item.NetsuiteToLocationInternalId)];
            }

            if (!hasOtherItemsWithSameSubsidiary && Document.ToSubsidiaries is not null)
            {
                Document.ToSubsidiaries = [.. Document.ToSubsidiaries
                .Where(d => d.NetsuiteSubsidiaryInternalId != item.NetsuiteToSubsidiaryInternalId)];
            }

            // Option B (Alternative): If unselecting should clear ALL selected destinations completely, use this instead:
            // Document.Destinations = [];
        }
        else
        {
            SelectedFulfillments.Add(item);

            if (!Document.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId == item.NetsuiteOrderInternalId))
                Document.ItemFulfillments = [.. Document.ItemFulfillments, item];

            var matchingDestination = DestinationLocations
                ?.FirstOrDefault(d => d.NetsuiteLocationInternalId == item.NetsuiteToLocationInternalId);

            var currentDestinations = Document.Destinations ?? [];
            if (!currentDestinations.Any(d => d.NetsuiteLocationInternalId == item.NetsuiteToLocationInternalId))
            {
                Document.Destinations = [.. currentDestinations, matchingDestination];
            }

            var matchingSubsidiary = ToSubsidiaries 
                ?.FirstOrDefault(s => s.NetsuiteSubsidiaryInternalId == item.NetsuiteToSubsidiaryInternalId);

            var currentSubsidiaries = Document.ToSubsidiaries ?? [];
            if (!currentSubsidiaries.Any(s => s.NetsuiteSubsidiaryInternalId == item.NetsuiteToSubsidiaryInternalId))
            {
                Document.ToSubsidiaries = [.. currentSubsidiaries, matchingSubsidiary];
            }
        }

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(FulfillmentsDataGrid.DataGrid);
    }
}
