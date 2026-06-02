using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Receiving;

public interface IReceivingIntegration
{
    public Task<(IEnumerable<PurchaseOrderInfoNSDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent);
    public Task<PurchaseOrderInfoNSDTO?> GetPurchaseOrderHeaderAsync(int docEntry);
    public Task<IEnumerable<PurchaseOrderLineNSDTO>> GetPurchaseOrderLinesAsync(int docEntry);
    public Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent);
    public Task<PurchaseDeliveryNoteHeaderSAPDTO?> GetPurchaseDeliveryNoteHeaderAsync(int docEntry);
    public Task<IEnumerable<PurchaseDeliveryNoteLineSAPDTO>> GetPurchaseDeliveryNoteLinesAsync(int docEntry);
    public Task<bool> PostGoodsReceiptPOAsync(PurchaseDeliveryNoteDTO data);
    public Task<IEnumerable<PurchaseTypeSAPDTO>> GetPurchaseTypesAsync();
}
