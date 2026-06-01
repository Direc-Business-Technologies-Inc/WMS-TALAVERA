using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;
public class ReceivingIntegration : IReceivingIntegration
{
    public Task<PurchaseDeliveryNoteHeaderSAPDTO?> GetPurchaseDeliveryNoteHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<PurchaseDeliveryNoteLineSAPDTO>> GetPurchaseDeliveryNoteLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<PurchaseOrderHeaderSAPDTO?> GetPurchaseOrderHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<PurchaseOrderLineSAPDTO>> GetPurchaseOrderLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<PurchaseOrderSAPDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<PurchaseTypeSAPDTO>> GetPurchaseTypesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsReceiptPOAsync(PurchaseDeliveryNoteDTO data)
    {
        throw new NotImplementedException();
    }
}