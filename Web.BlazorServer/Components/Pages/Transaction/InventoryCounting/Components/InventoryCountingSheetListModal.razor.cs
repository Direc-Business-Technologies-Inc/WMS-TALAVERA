using Microsoft.AspNetCore.Components;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;

public partial class InventoryCountingSheetListModal
{
    [Parameter] public required IEnumerable<InventoryCountingSheetVM> Sheets { get; set; }
    [Parameter] public Guid DocumentId { get; set; }

    void ViewSheet(InventoryCountingSheetVM sheet) =>
        NavManager.NavigateTo(
            $"/transactions/inventory/inventory-counting/sheet/view?SheetNo={Uri.EscapeDataString(sheet.SheetNo.Value)}&Document={DocumentId}",
            true);
}
