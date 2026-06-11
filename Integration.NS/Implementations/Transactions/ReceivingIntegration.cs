using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.NS.Services;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public async Task<(IEnumerable<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>, int)> GetTransferOrderLinesAsync(string Id, DataGridIntent intent)
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

        var response = await netsuiteService.ExecuteSuiteQLQuery<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>(query.Query, limit: query.Limit, offset: query.Offset);
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
                ("t.id", "SourceInternalId"),
                ("t.tranid", "CreatedFrom"),
                ("s.custrecord_dbti_default_bo_location", "DefaultBO"),
                ("t.custbody_dbti_receiving_category", "ReceivingCategory"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("s.name", "Subsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "ToSubsidiary"),
                ("vba.custrecord_dbti_vba_assigned_bin", "VendorPrefferedBin"),
                ("CASE WHEN t.custbody_dbti_transfer_category IN (3,4) THEN BUILTIN.DF(t.custbody_dbti_return_to_vendor) ELSE BUILTIN.DF(t.entity) END", "Vendor"),
                ("BUILTIN.DF(t.location)", "Location"),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),
                ("CASE WHEN t.custbody_dbti_transfer_category IN (3,4) THEN \'Returns\' ELSE t.type END", "Type"),
                ("t.custbody_dbti_prepared_by", "PreparedBy")
            )
            .From("transaction t")
            .Join("customrecord_dbti_vendor_bin_assignment vba", on: "t.entity = vba.custrecord_dbti_vba_vendor")
            .Join("subsidiary s", on:"t.subsidiary = s.id")
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
                ("tl.id", "LineNumber"),
                ("uom.unitname", "UoM"),
                ("loc.name", "Location"),
                ("loc.usebins", "LocationUsesBins"),
                ("item.displayname", "ItemDescription"),
                ("pb.bin", "PrefferedBinAssignmentId"),
                ("(tl.quantity / uom.conversionrate)", "QuantityPlanned"),
                ("(tl.quantity - tl.quantityshiprecv)", "QuantityOpen"),
                ("(tl.quantityshiprecv / uom.conversionrate)", "QuantityReceived")
            )
            .From("transactionline tl")
            .Join("item", on: "tl.item = item.id")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("location loc", on: "tl.location = loc.id")
            .Join("unitstypeuom uom", on: "tl.units = uom.internalid") 
            .LeftJoin("(SELECT ibq.bin, ibq.item, b.location FROM itembinquantity ibq JOIN bin b ON ibq.bin = b.id WHERE preferredbin = \'T\') pb", on: "pb.item = item.id AND pb.location = tl.location")
            .WithFilters(
                Equal("t.tranid", docEntry),
                Equal("tl.mainline", "F")
            );

        if (transferorder)
        {
            builder = builder.WithFilters(Equal("tl.transactionlinetype", "RECEIVING"));
        }

        var query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemReceiptLineDTO>(query.Query);
        return [.. response.items];
    }

    public async Task<bool> PostItemReceipt(ItemReceiptDTO dto)
    {
        var payload = ItemReceiptTransformPayload.Create(dto);
        var uri = $"{netsuiteService.GetRestAPIURI()}/record/v1/purchaseOrder/{dto.SourceInternalId}/!transform/itemReceipt";

        var payloadString = CreatePOJson(dto);
        var x = await netsuiteService.MakeRequest<string>(uri, payloadString, HttpMethod.Post);
        return true;
    }

    private string CreatePOJson(ItemReceiptDTO dto)
    {
        bool isGood = dto.Category.Equals(ItemReceiptDTO.ReceivingCategory.Good);
        var obj = new
        {
            custbody_dbti_receiving_category = isGood ? 1 : 2,
            item = new
            {
                items = dto.Lines.Select(line =>
                {
                    bool isItemReceived = line.IsReceived && line.Quantity > 0;
                    string? preferredBin = line.IsLocationBinUsed ? (isGood ? (dto.VendorPrefferedBin != 0 ? $"{dto.VendorPrefferedBin}" : $"{line.PrefferedBinAssignmentId}") : "5") : null;
                    return new
                    {
                        itemreceive = isItemReceived,
                        orderLine = line.LineNumber,
                        quantity = isItemReceived ? line.Quantity : (decimal?)null,
                        custcol_dbti_actual_weight = isItemReceived ? line.WeightReceived : (decimal?)null,
                        rate = isGood ? (decimal?) null : 0,
                        inventoryDetail = isItemReceived ? new
                        {
                            inventoryAssignment = new
                            {
                                items = new[]
                                {
                                    new
                                    {
                                        inventoryStatus = isGood ? "1" : "3",
                                        binNumber = isGood ? preferredBin : "5",
                                        quantity = line.Quantity
                                    }
                                }
                            }
                        } : null,
                        location = isGood ? (int?)null : dto.DefaultBO
                    };
                })
            },
            memo = "Created via WMS"
        };

        return JsonSerializer.Serialize(obj, JSON_OPTS);
    }

    readonly JsonSerializerOptions JSON_OPTS = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}