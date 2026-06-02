using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.NS.Services;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.NS.Implementations.Transactions;
public class ReceivingIntegration (
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory)
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

    public async Task<(IEnumerable<PurchaseOrderInfoNSDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
    var queryString = """
            SELECT 
                t.id AS Id,
                t.tranid AS ReferenceNumber,
                t.status AS Status,
                TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS Date,
                entity.altname AS VendorName,
                entity.entityid AS VendorCode,
                t.memo as Memo
            FROM 
                transaction t
            JOIN 
                entity ON entity.id = t.entity
            """;

        Dictionary<string, string> propertyMap = new()
        {
            { "CardName", "entity.altname" },
            { "DocNum", "t.id" },
            { "DocStatus", "t.status" },
            { "DocDate", "t.createdDate" },
            { "Remarks", "t.memo"}
        };

        var builder = builderFactory.Create(queryString)
            .ApplyDataGridIntent(intent, propertyMap)
            .AddFilter(new AppFilterDescriptor
            {
                Property = "t.recordtype",
                ComparisonOperator = ComparisonOperatorEnum.Equals,
                Value = "purchaseorder"
            }, propertyMap);
        SuiteQLQuery query = builder.Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderInfoNSDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
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