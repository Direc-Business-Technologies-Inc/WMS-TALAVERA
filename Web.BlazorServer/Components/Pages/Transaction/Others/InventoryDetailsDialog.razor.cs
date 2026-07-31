using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Pages.Transaction.Others;

public partial class InventoryDetailsDialog
{
    [Inject] public ILocationHandler locationHandler { get; set; } = default!;
    [Inject] public IInventoryHandler inventoryHandler { get; set; } = default!;
    [Parameter][EditorRequired] public int LocationId { get; set; }
    [Parameter][EditorRequired] public decimal Amount { get; set; }
    [Parameter] public Types Type { get; set; } = Types.Outgoing;
    [Parameter] public int? ItemId { get; set; } = null;
    [Parameter] public int? StatusId { get; set; } = null;
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public List<AppFilterDescriptor> StatusFilters { get; set; } = [];
    [Parameter] public List<InventoryDetailVM> InventoryDetails { get; set; } = [];
    // this was a bad idea probably but it allows the inventory details dialog to load inventory details from the db
    // the first int in the tuple corresponds to the transaction id and the second corresponds to the line number
    [Parameter] public Tuple<int, int>? LoadInventoryDetails { get; set; } = null;

    List<DetailItem> Details = [];
    List<InventoryBalanceVM> InventoryBalance = [];

    string? ErrorString = null;

    readonly string ActionGetLocation = "Get Location";
    readonly string ActionGetInventoryBalance = "Get Inventory Balance";
    Task InitTask = Task.CompletedTask;

    decimal AmountSum => Details.Sum(x => Math.Abs(x.Detail.QuantityAlloted));

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetLocation);
    bool LocationHasBins = true;
    bool IsLoadingDetails = false;


    protected override async Task OnParametersSetAsync()
    {
        InitTask = LoadBalances();
        IsLoadingDetails = LoadInventoryDetails is not null;
        await Task.WhenAll(
            LoadLocation(),
            base.OnParametersSetAsync()
        );
        Details.AddRange(InventoryDetails.Select(CreateDetailItem));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && LoadInventoryDetails is not null)
        {
            IsLoadingDetails = true;
            await InvokeAsync(StateHasChanged);

            var inventoryDetails = await inventoryHandler.GetInventoryDetails(
                LoadInventoryDetails.Item1,
                LoadInventoryDetails.Item2);

            Details.AddRange(inventoryDetails.Select(CreateDetailItem));

            IsLoadingDetails = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadLocation()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            var location = await locationHandler.GetLocation(LocationId);
            LocationHasBins = location?.HasBins ?? false;
        }, ActionGetLocation);
    }

    async Task LoadBalances()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            var location = await locationHandler.GetLocation(LocationId);
            (var data, int count) = await inventoryHandler.GetInventoryBalanceAsync(
                new() { Take = -1 }, 
                locationId: LocationId,
                itemId: ItemId,
                statusId: StatusId 
            );

            InventoryBalance = [.. data];
        }, ActionGetInventoryBalance);

        await InvokeAsync(StateHasChanged);
    }


    async Task RemoveLine(DetailItem item)
    {
        Details.Remove(item);

        await InvokeAsync(StateHasChanged);
    }

    async Task<(IEnumerable<LocationBinVM>, int)> LocationBinProvider(DataGridIntent intent)
    {
        await InitTask;

        if (Type == Types.Outgoing)
        {
            if (!LocationHasBins) return ([], 0);

            var bins = InventoryBalance.Where(x => x.Bin != null)
                .DistinctBy(x => x.Bin!.Id)
                .Select(x => x.Bin!);

            return ([.. bins], bins.Count());
        }

        return await locationHandler.GetLocationBinsAsync(LocationId, intent);
    }
    async Task<(IEnumerable<InventoryStatusVM>, int)> StatusProvider(DetailItem item, DataGridIntent intent)
    {
        await InitTask;

        if (Type == Types.Outgoing)
        {

            if (item.Detail.Bin is null && LocationHasBins) return ([], 0);

            var binId = item.Detail.Bin?.Id ?? -1;
            var balanceSelection = LocationHasBins ?
                InventoryBalance.Where(x => x.Bin?.Id == binId && x.Status != null) :
                InventoryBalance.Where(x => x.Bin is null && x.Status != null);

            var statuses = balanceSelection 
                .DistinctBy(x => x.Status!.Id)
                .Select(x => x.Status!);

            return ([.. statuses], statuses.Count());
        }

        var newIntent = intent.Adapt<DataGridIntent>();
        if (StatusFilters.Count > 0)
        {
            newIntent.Filters.AddRange(StatusFilters);
        }
        return await inventoryHandler.GetInventoryStatusAsync(newIntent);
    }

    async Task Submit()
    {
        if (Amount < AmountSum)
        {
            ErrorString = "Quantity of items alloted exceed the expected count";
            return;
        }

        if (Amount > AmountSum)
        {
            ErrorString = "Quantity of items alloted does not meet the expected count";
            return;
        }


        List<InventoryDetailVM> details = [..Details.Where(x => x.Detail.QuantityAlloted != 0).Select(x => x.Detail)];

        DialogService.Close(details);
    }

    async Task AddItem()
    {
        Details.Add(new(this));
        await InvokeAsync(StateHasChanged);
    }
    public enum Types
    {
        Outgoing,
        Incoming
    }

    DetailItem CreateDetailItem(InventoryDetailVM vm)
    {
        return new DetailItem(this)
        {
            Detail = vm
        };
    }

    class DetailItem(InventoryDetailsDialog parent)
    {
        public InventoryDetailVM Detail { get; set; } = new();
        public QuickVirtualizedDropdown<InventoryStatusVM>? StatusDropdown { get; set; }

        public async Task BinSet(LocationBinVM? bin)
        {
            Detail.Bin = bin;
            StatusDropdown?.Reset();
        }
        public decimal? MaxValue => parent.Type == Types.Incoming ? null : Math.Min(QuantityOnHand, parent.Amount);
        public decimal QuantityOnHand => parent.InventoryBalance
            .Where(x =>
                x.Status?.Id == Detail.Status?.Id &&
                x.Bin?.Id == Detail.Bin?.Id
            )
            .Sum(x => x.QuantityOnHand);
    }
}