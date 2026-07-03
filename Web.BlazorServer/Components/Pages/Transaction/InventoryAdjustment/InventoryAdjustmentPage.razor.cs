using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentPage
{
    [Inject] public IInventoryAdjustmentHandler? InventoryAdjustmentHandler { get; set; }

    readonly string ActionGetInventoryAdjustment = "Get Inventory Adjustment";

    async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> LoadDataAsync(DataGridIntent intent)
    {
        if (InventoryAdjustmentHandler is null) throw new Exception("No handlers registered for inventory adjustment");

        return await InventoryAdjustmentHandler.GetInventoryAdjustmentsDataGridAsync(intent);
    }

    void ViewInventoryAdjustment(InventoryAdjustmentDataGridVM item)
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.VIEW + $"?ref={item.ReferenceNumber}");
    }
}
