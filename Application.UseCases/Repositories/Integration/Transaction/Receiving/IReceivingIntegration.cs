using Application.DataTransferObjects.Transactions.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Receiving;

public interface IReceivingIntegration
{
    public Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent);
    public Task<(IEnumerable<TransferOrderDataGridDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent);
    public Task<(IEnumerable<ReturnsDataGridDTO>, int count)> GetReturnsListAsync(DataGridIntent intent);
    public Task<PurchaseOrderDTO?> GetPurchaseOrderHeaderAsync(string docEntry);
    public Task<TransferOrderDTO?> GetTransferOrderHeaderAsync(string docEntry);
    public Task<ReturnsDTO?> GetReturnsHeaderAsync(string docEntry);
    public Task<ItemReceiptDTO?> GetItemReceiptHeaderAsync(string docEntry);
    public Task<(IEnumerable<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>, int)> GetTransferOrderLinesAsync(string Id, DataGridIntent intent);
    public Task<IEnumerable<PurchaseOrderLineDTO>> GetPurchaseOrderLinesAsync(string docEntry);
    public Task<IEnumerable<ReturnsLineDTO>> GetReturnsLinesAsync(string docEntry);
    public Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptLinesAsync(string docEntry, bool isTransferOrder = false);
    public Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent);
    public Task<bool> PostItemReceipt(ItemReceiptDTO itemReceiptDTO);
    public Task<BarcodeDTO?> GetBarcodeData(string barcode);
}
