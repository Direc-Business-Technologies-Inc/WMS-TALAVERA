using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Components.Custom;

partial class ItemSelectionDialog
{
    [Inject] DialogService? DialogService { get; set; }
    [Parameter] public int? Location { get; set; } = null;
    [Parameter] public List<AppFilterDescriptor> Filters { get; set; } = [];
    async Task OnItemsSelected(List<ItemsVM> items)
    {
        DialogService?.Close(items);
    }
}
