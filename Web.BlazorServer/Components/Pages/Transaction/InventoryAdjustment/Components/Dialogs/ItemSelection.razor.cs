using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.Dialogs;

public partial class ItemSelection
{
    [Parameter] public int? Location { get; set; } = null;
    [Parameter] public bool IsReceipt { get; set; } = false;

    readonly List<AppFilterDescriptor> ReceiptFilters = [
        DataGridFilterUtilities.GreaterThan("QuantityOnHand", 0)
    ];
    readonly List<AppFilterDescriptor> IssueFilters = [
        DataGridFilterUtilities.GreaterThan("QuantityOnHand", 0)
    ];
    List<AppFilterDescriptor> Filters => IsReceipt ? ReceiptFilters : IssueFilters;
    int? LocationFilter => IsReceipt ? null : Location;

    async Task OnItemsSelected(List<ItemsVM> items)
    {
        DialogService.Close(items);
    }
}
