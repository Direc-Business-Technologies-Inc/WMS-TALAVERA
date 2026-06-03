using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;

public interface IReceivingHandler
{
    Task<(IEnumerable<ReceivingPurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrderDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ReceivingTransferOrderDataGridVM> Data, int Count)> GetTransferOrderDataGridAsync(DataGridIntent intent);
    Task<PurchaseOrderVM?> GetPurchaseOrderAsync(int docEntry);
    Task<TransferOrderVM?> GetTransferOrderAsync(int docEntry);
    Task<(IEnumerable<PurchaseDeliveryNoteDataGridVM> Data, int Count)> GetPurchaseDeliveryNoteDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<TransferOrderLineVM> Data, int Count)> GetTransferOrderLinesDataGridAsync(int transferOrderId, DataGridIntent intent);
    Task<PurchaseDeliveryNoteVM?> GetPurchaseDeliveryNoteAsync(int docEntry);
    Task<bool> PostGoodsReceiptPOAsync(PurchaseOrderVM data);
    Task<IEnumerable<PurchaseTypeVM>> GetPurchaseTypesAsync();
}
