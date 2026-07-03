using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;

public interface IReceivingHandler
{
    Task<(IEnumerable<PurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrderDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<TransferOrderDataGridVM> Data, int Count)> GetTransferOrderDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ReturnsDataGridVM> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent);
    Task<PurchaseOrderVM?> GetPurchaseOrderAsync(string docEntry);
    Task<TransferOrderVM?> GetTransferOrderAsync(string docEntry);
    Task<ItemReceiptVM?> GetItemReceiptSourceAsync(string docEntry);
    Task<ReturnsVM?> GetReturnsAsync(string docEntry);
    Task<(IEnumerable<TransferOrderLineVM> Data, int Count)> GetTransferOrderLinesDataGridAsync(string transferOrderId, DataGridIntent intent);
    Task<bool> PostItemReceipt(ItemReceiptVM Data);
    Task<bool> PostGoodsReceiptPOAsync(PurchaseOrderVM data);
    Task<BarcodeVM?> GetBarcodeData(string barcode);

}
