using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Receiving;

public interface IReceivingIntegration
{
    public Task<(IEnumerable<ReceivingInfoNSDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent);
    public Task<(IEnumerable<ReceivingInfoNSDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent);
    public Task<ReceivingInfoNSDTO?> GetPurchaseOrderHeaderAsync(int docEntry);
    public Task<ReceivingInfoNSDTO?> GetTransferOrderHeaderAsync(int docEntry);
    public Task<(IEnumerable<ReceivingLineNSDTO>, int)> GetTransferOrderLinesAsync(int Id, DataGridIntent intent);
    public Task<IEnumerable<ReceivingLineNSDTO>> GetPurchaseOrderLinesAsync(int docEntry);
    public Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent);
    public Task<PurchaseDeliveryNoteHeaderSAPDTO?> GetPurchaseDeliveryNoteHeaderAsync(int docEntry);
    public Task<IEnumerable<PurchaseDeliveryNoteLineSAPDTO>> GetPurchaseDeliveryNoteLinesAsync(int docEntry);
    public Task<bool> PostGoodsReceiptPOAsync(PurchaseDeliveryNoteDTO data);
    public Task<IEnumerable<PurchaseTypeSAPDTO>> GetPurchaseTypesAsync();
}
