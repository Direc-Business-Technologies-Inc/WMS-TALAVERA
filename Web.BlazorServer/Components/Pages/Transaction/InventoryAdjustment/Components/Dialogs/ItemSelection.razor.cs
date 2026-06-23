using Microsoft.AspNetCore.Components;
using Radzen;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.Dialogs;

public partial class ItemSelection
{
    [Parameter] public int? Location { get; set; } = null;
    async Task OnItemsSelected(List<ItemsVM> items)
    {
        DialogService.Close(items);
    }
}
