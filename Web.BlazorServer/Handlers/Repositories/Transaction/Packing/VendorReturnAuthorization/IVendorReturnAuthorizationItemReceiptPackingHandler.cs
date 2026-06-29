using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;

public interface IVendorReturnAuthorizationItemReceiptPackingHandler
{
    Task<VendorReturnAuthorizationItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry);
    Task<bool> PostItemReceipt(VendorReturnAuthorizationItemReceiptPackingVM data);
}
