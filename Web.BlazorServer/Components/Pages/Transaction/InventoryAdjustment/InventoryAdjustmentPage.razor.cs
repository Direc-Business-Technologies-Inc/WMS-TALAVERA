using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentPage
{
    [Inject] public IInventoryAdjustmentHandler? InventoryAdjustmentHandler { get; set; }
    [SupplyParameterFromQuery]
    string? Type
    {
        get => _tab;
        set
        {
            _tab = value;
            TabIndex = value?.Equals("issues", StringComparison.OrdinalIgnoreCase) ?? false ? 1 : 0;
        }
    }

    readonly string ActionGetInventoryAdjustment = "Get Inventory Adjustment";
    int TabIndex = 0;
    string? _tab;

    async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> IssuesProvider(DataGridIntent intent)
    {
        if (InventoryAdjustmentHandler is null) throw new Exception("No handlers registered for inventory adjustment");

        if (intent.Sorts.Count == 0)
        {
            intent.Sorts.Add(new()
            {
                Property = "Date",
                Direction = SortDirectionEnum.Descending
            });

            intent.Sorts.Add(new()
            {
                Property = "ReferenceNumber",
                Direction = SortDirectionEnum.Descending
            });
        }

        return await InventoryAdjustmentHandler.GetIssuesDataGridAsync(intent);
    }

    async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> ReceiptsProvider(DataGridIntent intent)
    {
        if (InventoryAdjustmentHandler is null) throw new Exception("No handlers registered for inventory adjustment");

        if (intent.Sorts.Count == 0)
        {
            intent.Sorts.Add(new()
            {
                Property = "Date",
                Direction = SortDirectionEnum.Descending
            });

            intent.Sorts.Add(new()
            {
                Property = "ReferenceNumber",
                Direction = SortDirectionEnum.Descending
            });
        }

        return await InventoryAdjustmentHandler.GetReceiptsDataGridAsync(intent);
    }

    void ViewInventoryAdjustment(InventoryAdjustmentDataGridVM item)
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.VIEW + $"?ref={item.ReferenceNumber}");
    }

    async Task OnTabChanged()
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX + $"?type={(TabIndex == 0 ? "receipts" : "issues")}");
    }
}
