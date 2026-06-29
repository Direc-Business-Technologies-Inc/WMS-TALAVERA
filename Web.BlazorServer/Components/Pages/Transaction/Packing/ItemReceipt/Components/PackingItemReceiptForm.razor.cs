using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.ViewModels.Transaction.Packing.ItemReceipt;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.ItemReceipt.Components;

partial class PackingItemReceiptForm
{
    [Parameter] public ItemReceiptPackingVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; }
    [Parameter] public EventCallback<ItemReceiptPackingVM> OnValidSubmit { get; set; }
    [Inject] NavigationManager NavManager { get; set; } = default!;

    public void Submit()
    {
        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
        {
            OnValidSubmit.InvokeAsync(Data);
        }
    }

    void Return()
    {
        NavManager.NavigateTo(Data.SourceType switch
        {
            ItemReceiptPackingVM.SourceTypes.TransferOrder => $"{PackingRoutes.StockTransferRequestView}?ref={Data.CreatedFrom}",
            _ => $"{PackingRoutes.Root}?tab=stocktransferrequest"
        });
    }
}
