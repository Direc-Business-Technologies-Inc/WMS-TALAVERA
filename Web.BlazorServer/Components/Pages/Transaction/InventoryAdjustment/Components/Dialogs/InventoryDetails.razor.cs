using Application.DataTransferObjects.Others;
using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.Dialogs;

public partial class InventoryDetails
{
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Parameter][EditorRequired] public required decimal TotalQuantity { get; set; }
    [Parameter][EditorRequired] public required LocationVM Location { get; set; }
    [Parameter] public List<InventoryDetailVM> Details { get; set; } = [];
    [Parameter] public EventCallback<List<InventoryDetailVM>> DetailsChanged { get; set; }

    string ActionGetLocationBins => $"Get Bins For Location {Location.Name}";
    private List<InventoryDetailVM> _details = [];
    decimal CurrentCount => _details.Where(x => x.Bin is not null).Sum(detail => detail.QuantityAlloted);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _details = [.. Details];
    }

    async Task<(IEnumerable<LocationBinVM>, int)> LocationBinProvider(DataGridIntent intent)
    {
        return await locationHandler.GetLocationBinsAsync(Location.Id, intent);
    }

    async Task AddLine()
    {
        Details.Add(new());
        DialogService.Refresh();
        await InvokeAsync(StateHasChanged);
    }

    async Task Save()
    {
        if (CurrentCount != TotalQuantity)
        {
            ToastService.Warning(header: "Incorrect number of items", message: $"Total number of items must equal {TotalQuantity}");
            return;
        }

        Details = [.. _details.Where(x => x.QuantityAlloted > 0 && x.Bin is not null)];
        await DetailsChanged.InvokeAsync(Details);
        DialogService.Close(Details);
    }
}
