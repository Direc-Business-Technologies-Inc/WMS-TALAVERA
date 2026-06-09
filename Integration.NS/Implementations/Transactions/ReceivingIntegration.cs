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
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

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

    public async Task<PurchaseOrderDTO?> GetPurchaseOrderHeaderAsync(string docEntry)
    {
        var queryString = $"""
            SELECT 
                t.id AS Id,
                t.tranid AS ReferenceNumber,
                TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS Date,
                TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS Date,
                entity.altname AS VendorName,
                t.memo as Memo
            FROM 
                transaction t
            JOIN 
                entity ON entity.id = t.entity
            WHERE
                t.tranid = '{docEntry}'
            """;

        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderDTO>(queryString);
        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO>> GetPurchaseOrderLinesAsync(string docEntry)
    {
        var queryString = $"""
            SELECT
                item.itemId AS ItemCode,
                BUILTIN.DF(tl.units) as UoM,
                BUILTIN.DF(tl.location) as Location,
                item.displayname AS ItemDescription,
                tl.quantity AS QuantityPlanned
            FROM
                transaction t
            JOIN 
                transactionline tl ON tl.transaction = t.id
            JOIN
                item ON item.id = tl.item
            WHERE
                t.tranid = '{docEntry}' AND
                tl.mainline = 'F'

            """;

        var response = await netsuiteService.ExecuteSuiteQLQuery<Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO>(queryString);

        return response.items;
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("t.status", "Status"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("t.location", "Location"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DeliveryDate"),
                ("t.memo", "Memo"),
                ("BUILTIN.DF(t.entity)", "VendorName"),
                ("t.transferlocation", "TransferLocation"))
            .From("transaction t")
            .WithDatagridIntent(intent)
            .WithFilters(
                Equal("t.recordtype", "purchaseorder"),
                In("t.status", new string[] {"B", "E" })
            );

        SuiteQLQuery query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderDataGridDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<TransferOrderDataGridDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("t.status", "Status"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "SourceSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation")
                )
            .From("transaction t")
            .Join("transactionline tl", on:"tl.transaction = t.id")
            .WithFilters(
                Equal("tl.mainline", "T"),
                In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }),
                NotEqual("t.custbody_dbti_transfer_category", 4), // returns - bad items
                NotEqual("t.custbody_dbti_transfer_category", 3), // returns - good items
                In("t.status", new string[] {"F", "E"}))
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TransferOrderDataGridDTO>(query.Query, limit: query.Limit, offset: query.Offset);
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

    public async Task<TransferOrderDTO?> GetTransferOrderHeaderAsync(string docEntry)
    { 
        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("t.status", "Status"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "FromSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "ToSubsidiary"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),
                ("t.custbody_dbti_prepared_by", "PreparedBy")
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .WithFilters(
                In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }),
                Equal("tl.mainline", "T"),
                Equal("t.tranid", docEntry),
                NotEqual("t.custbody_dbti_transfer_category", 3),
                NotEqual("t.custbody_dbti_transfer_category", 4),
                In("t.status", new string[] {"F", "E"})
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TransferOrderDTO>(query.Query);
        return response.items.FirstOrDefault();
    }

    public async Task<(IEnumerable<ReceivingLineNSDTO>, int)> GetTransferOrderLinesAsync(string Id, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", "ItemCode"),
                ("BUILTIN.DF(tl.units)", "UoM"),
                ("BUILTIN.DF(tl.location)", "Warehouse"),
                ("item.displayname", "ItemDescription"),
                ("tl.quantity", "QuantityPlanned"),
                ("tl.quantityshiprecv", "QuantityReceived"),
                ("(tl.quantity - tl.quantityshiprecv)", "QuantityOpen"))
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .WithFilters(
                NotEqual("t.custbody_dbti_transfer_category", 3),
                NotEqual("t.custbody_dbti_transfer_category", 4),
                Equal("t.tranid", Id),
                Equal("tl.transactionlinetype", "RECEIVING")
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReceivingLineNSDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<ReturnsDataGridDTO>, int count)> GetReturnsListAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "SourceSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"),
                ("t.custbody_dbti_return_to_vendor", "VendorName"),
                ("BUILTIN.DF(t.location)", "Location"),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),
                ("t.memo", "Memo")
            )
            .From("transaction t")
            .WithDatagridIntent(intent)
            .WithFilters(
                Equal("t.recordtype", "intercompanytransferorder"),
                In("t.status", new string[] { "F", "E" }),
                Any(
                    Equal("t.custbody_dbti_transfer_category", 3),
                    Equal("t.custbody_dbti_transfer_category", 4))
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReturnsDataGridDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<ReturnsDTO?> GetReturnsHeaderAsync(string docEntry)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "FromSubsidiary"),
                ("t.custbody_dbti_return_to_vendor", "Vendor"),
                ("BUILTIN.DF(t.location)", "FromWarehouse"),
                ("BUILTIN.DF(t.transferlocation)", "ToWarehouse"),
                ("t.custbody_dbti_prepared_by", "PreparedBy")
            )
            .From("transaction t")
            .WithFilters(
                Equal("t.recordtype", "intercompanytransferorder"),
                Any(
                    Equal("t.custbody_dbti_transfer_category", 3),
                    Equal("t.custbody_dbti_transfer_category", 4)),
                Equal("t.tranid", docEntry)
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReturnsDTO>(query.Query);
        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<ReturnsLineDTO>> GetReturnsLinesAsync(string docEntry)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemid", "ItemCode"),
                ("BUILTIN.DF(tl.units)", "UoM"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("item.displayname", "ItemDescription"),
                ("tl.quantity", "QuantityPlanned")
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .WithFilters(
                Equal("tl.transactionlinetype", "RECEIVING"),
                Equal("t.tranid", docEntry),
                Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReturnsLineDTO>(query.Query);
        return [.. response.items];
    }

    public async Task<ItemReceiptDTO?> GetItemReceiptHeaderAsync(string docEntry)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.tranid", "CreatedFrom"),
                ("t.custbody_dbti_receiving_category", "ReceivingCategory"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "Subsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "ToSubsidiary"),
                ("CASE WHEN t.custbody_dbti_transfer_category IN (3,4) THEN BUILTIN.DF(t.custbody_dbti_return_to_vendor) ELSE BUILTIN.DF(t.entity) END", "Vendor"),
                ("BUILTIN.DF(t.location)", "Location"),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),
                ("CASE WHEN t.custbody_dbti_transfer_category IN (3,4) THEN \'Returns\' ELSE t.type END", "Type"),
                ("t.custbody_dbti_prepared_by", "PreparedBy")
            )
            .From("transaction t")
            .WithFilters(
                In("t.type", new string[] {"TrnfrOrd", "PurchOrd" }),
                Equal("t.tranid", docEntry)
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemReceiptDTO>(query.Query);
        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptLinesAsync(string docEntry, bool transferorder = false)
    {
        var builder = builderFactory.Create()
            .Select(
                ("item.itemid", "ItemCode"),
                ("BUILTIN.DF(tl.units)", "UoM"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("item.displayname", "ItemDescription"),
                ("tl.quantity", "QuantityPlanned"),
                ("tl.quantityshiprecv", "QuantityReceived")
            )
            .From("transactionline tl")
            .Join("item", on: "tl.item = item.id")
            .Join("transaction t", on: "tl.transaction = t.id")
            .WithFilters(
                Equal("t.tranid", docEntry),
                NotEqual("tl.mainline", "T")
            );

        if (transferorder)
        {
            builder = builder.WithFilters(Equal("tl.transactionlinetype", "RECEIVING"));
        }

        var query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemReceiptLineDTO>(query.Query);
        return [.. response.items];
    }
}