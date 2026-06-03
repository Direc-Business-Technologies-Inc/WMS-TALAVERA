using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Domain.Entities.ValueObjects.Others;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.NS.Implementations.Transactions;

public class ReceivingIntegration(
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

    public async Task<ReceivingInfoNSDTO?> GetPurchaseOrderHeaderAsync(int docEntry)
    {
        var queryString = $"""
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
            WHERE
                t.id = {docEntry}
            """;

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReceivingInfoNSDTO>(queryString);
        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<ReceivingLineNSDTO>> GetPurchaseOrderLinesAsync(int docEntry)
    {
        var queryString = $"""
            SELECT
                item.itemId AS ItemCode,
                BUILTIN.DF(tl.units) as UoM,
                BUILTIN.DF(tl.location) as Warehouse,
                item.displayname AS ItemDescription,
                tl.quantity AS QuantityPlanned,
                tl.quantityshiprecv AS QuantityReceived,
                (tl.quantity - tl.quantityshiprecv) AS QuantityOpen
            FROM
                transaction t
            JOIN 
                transactionline tl ON tl.transaction = t.id
            JOIN
                item ON item.id = tl.item
            WHERE
                t.id = {docEntry}
            """;

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReceivingLineNSDTO>(queryString);

        return response.items;
    }

    public async Task<(IEnumerable<ReceivingInfoNSDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("t.status", "Status"),
                ("TO_CHAR(t.createdDate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("entity.altname", "VendorName"),
                ("entity.entityid", "VendorCode"),
                ("t.memo", "Memo"))
            .From("transaction t")
            .Join("entity", "entity.id = t.entity")
            .WithDatagridIntent(intent)
            .WithFilter(new AppFilterDescriptor
            {
                Property = "t.recordtype",
                ComparisonOperator = ComparisonOperatorEnum.Equals,
                Value = "purchaseorder"
            });

        SuiteQLQuery query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReceivingInfoNSDTO>(query.Query, limit: query.Limit, offset: query.Offset);
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