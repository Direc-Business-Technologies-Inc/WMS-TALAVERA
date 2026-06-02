using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.NS.Implementations.Transactions;
public class ReceivingIntegration (INetSuiteApiClientService netsuiteService)
    : IReceivingIntegration
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

    public async Task<(IEnumerable<PurchaseOrderSAPDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        var query = """
            SELECT 
                t.id AS DocNum,
                t.tranid AS SupplierContactPerson,
                TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS DocDate,
                t.status AS DocStatus,
                entity.altname AS CardName,
                entity.email AS SupplierContactPerson,
                entity.entityid AS CardCode,
                t.memo as Remarks
            FROM 
                transaction t
            JOIN 
                entity ON entity.id = t.entity
            WHERE 
                t.recordtype = 'purchaseorder'
            ORDER BY 
                t.trandate DESC, t.id
            """;
        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderSAPDTO>(query, intent.Take, intent.Skip);
        return (response.items, response.count);
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