using Web.BlazorServer.ViewModels.Transaction.Packing.ItemReceipt;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.ItemReceipt;

public interface IItemReceiptPackingHandler
{
    Task<ItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry);
    Task<bool> PostItemFulfillment(ItemReceiptPackingVM data);
}
