using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class InventoryDetailsDialog
{
    [Parameter]
    [EditorRequired]
    public LocationVM Location { get; set; }
    [Parameter]
    [EditorRequired]
    public int ItemId { get; set; }
    [Parameter]
    [EditorRequired]
    public decimal TotalQuantity { get; set; }


    [Inject] ILocationHandler locationHandler { get; set; } = default!;

    readonly string ActionGetInventoryBalance = "Get Inventory Balance";

    List<LocationBinVM> Bins = [];
    List<AllocationSelection> Details = [];
    Dictionary<int, List<InventoryBalanceVM>> BinAllocations = [];

    decimal CurrentCount => Details.Sum(x => x.QuantityAlloted);
    bool IsFull => CurrentCount == TotalQuantity;
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadBalance();
    }

    public async Task LoadBalance()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            DataGridIntent intent = new DataGridIntent {Take = -1};
            intent.Filters.Add(
                DataGridFilterUtilities.Equal(nameof(InventoryBalanceVM.ItemId), ItemId)
            );

            var balance = await locationHandler.GetLocationInventoryBalanceAsync(
                Location.Id, intent
            );

            return balance.Data.ToList();
        }, ActionGetInventoryBalance);

        action.OnSuccess(async (res) =>
        {
            InitBins(res);
            await InvokeAsync(StateHasChanged);
        });
    }

    private void InitBins(List<InventoryBalanceVM> balanceSheet)
    {

        Dictionary<int, string> binDict = new();
        balanceSheet.ForEach(x =>
        {

            if (x.Bin != null) {
                binDict[x.Bin.Id] = x.Bin.BinNumber;
                if (!BinAllocations.ContainsKey(x.Bin.Id)) BinAllocations[x.Bin.Id] = [];
                BinAllocations[x.Bin.Id].Add(x);
            }
        });

        Bins = [.. binDict.Select(x => new LocationBinVM {
            Id = x.Key,
            BinNumber = x.Value
        })];
    }


    async Task<(IEnumerable<InventoryBalanceVM>, int)> InventoryBalanceProvider(DataGridIntent intent, int bin)
    {
        if (!BinAllocations.ContainsKey(bin)) return ([], 0);

        return (BinAllocations[bin], BinAllocations[bin].Count);
    }

    async Task<(IEnumerable<LocationBinVM>, int)> BinProvider(DataGridIntent intent)
    {
        return (Bins, Bins.Count);
    }

    async Task AddLine()
    {
        Details.Add(new());
        await InvokeAsync(StateHasChanged);
    }

    async Task Save()
    {
        List<InventoryDetailVM> _details = Details.Select(x => new InventoryDetailVM
        {
            Bin = x.Bin,
            Status = x.Balance?.Status ?? null,
            QuantityAlloted = x.QuantityAlloted,
        }).ToList();

        DialogService.Close(_details);
    }

    private async Task BinSet(AllocationSelection selection, LocationBinVM? value)
    {
        selection.BinSet(value);
        await InvokeAsync(StateHasChanged);
    }

    private class AllocationSelection
    {
        public LocationBinVM? Bin { get; set; }
        public InventoryBalanceVM? Balance { get; set; }
        public string QuantityOnHand => Balance?.QuantityOnHand.ToString() ?? string.Empty;
        public string QuantityAvailable => Balance?.QuantityAvailable.ToString() ?? string.Empty;
        public string QuantityCommited => Balance?.QuantityCommited.ToString() ?? string.Empty;
        public decimal QuantityAlloted { get; set; }
        public QuickVirtualizedDropdown<InventoryBalanceVM>? BalanceDropdown { get; set; }

        public void BinSet(LocationBinVM? value)
        {
            Bin = value;
            QuantityAlloted = 0;
            Balance = null;
            BalanceDropdown?.Reset();
        }
    }
}
