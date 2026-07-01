using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;

public interface IReturnsItemReceiptPackingHandler
{
    Task<ReturnsItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry);
    Task<bool> PostItemFulfillment(ReturnsItemReceiptPackingVM data);
}
