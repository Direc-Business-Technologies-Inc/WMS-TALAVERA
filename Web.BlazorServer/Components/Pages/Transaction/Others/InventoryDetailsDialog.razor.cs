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

    List<DetailItem> Details = [];
    List<InventoryBalanceVM> InventoryBalance = [];

    string? ErrorString = null;

    readonly string ActionGetLocation = "Get Location";
    readonly string ActionGetInventoryBalance = "Get Inventory Balance";
    Task InitTask = Task.CompletedTask;

    decimal AmountSum => Details.Sum(x => x.Detail.QuantityAlloted);

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetLocation);
    bool LocationHasBins = true;


    protected override async Task OnParametersSetAsync()
    {
        InitTask = LoadBalances();
        await Task.WhenAll(
            LoadLocation(),
            base.OnParametersSetAsync()
        );
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
            (var data, int count) = await inventoryHandler.GetInventoryBalanceAsync(new() { Take = -1 }, locationId: LocationId);

            InventoryBalance = [.. data];
        }, ActionGetInventoryBalance);
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

        return await inventoryHandler.GetInventoryStatusAsync(intent);
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


        List<InventoryDetailVM> details = [..Details.Select(x => x.Detail)];

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

    class DetailItem(InventoryDetailsDialog parent)
    {
        public InventoryDetailVM Detail { get; set; } = new();
        public QuickVirtualizedDropdown<InventoryStatusVM>? StatusDropdown { get; set; }

        public async Task BinSet(LocationBinVM? bin)
        {
            Detail.Bin = bin;
            StatusDropdown?.Reset();
        }
        public decimal? MaxValue => parent.Type == Types.Incoming ? null : QuantityOnHand;
        public decimal QuantityOnHand => parent.InventoryBalance
            .Where(x =>
                x.Status?.Id == Detail.Status?.Id &&
                x.Bin?.Id == Detail.Bin?.Id
            )
            .Sum(x => x.QuantityOnHand);
    }
}