using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.NS.Services;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Shared.Libraries.ViewModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;
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

    public async Task<PurchaseOrderDTO?> GetPurchaseOrderHeaderAsync(string docEntry)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(PurchaseOrderDTO.Id)),
                ("t.tranid", nameof(PurchaseOrderDTO.ReferenceNumber)),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS') ", nameof(PurchaseOrderDTO.Date)),
                ("TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD\"T\"HH24:MI:SS') ", nameof(PurchaseOrderDTO.DeliveryDate)),
                ("BUILTIN.DF(t.entity)", nameof(PurchaseOrderDTO.VendorName)),
                ("t.memo", nameof(PurchaseOrderDTO.Memo))
            )
            .From("transaction t")
            .WithFilters(
                Equal("t.tranid", docEntry) 
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderDTO>(query.Query);
        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO>> GetPurchaseOrderLinesAsync(string docEntry)
    {
        var query = builderFactory.Create()
            .Select(
                ("tl.id", nameof(PurchaseOrderLineDTO.LineNumber)),
                ("BUILTIN.DF(tl.units)", nameof(PurchaseOrderLineDTO.UoM)),
                ("BUILTIN.DF(tl.location)", nameof(PurchaseOrderLineDTO.Location)),
                ("item.displayname", nameof(PurchaseOrderLineDTO.ItemDescription)),
                ("item.itemid", nameof(PurchaseOrderLineDTO.ItemCode)),
                ("tl.quantity", nameof(PurchaseOrderLineDTO.QuantityPlanned))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .Join("item", on: "item.id = tl.item")
            .WithFilters(
                Equal("t.tranid", docEntry),
                Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO>(query.Query);

        return response.items;
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("t.location", "Location"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DeliveryDate"),
                ("t.memo", "Memo"),
                ("BUILTIN.DF(t.entity)", "VendorName"),
                ("s.name", nameof(PurchaseOrderDataGridDTO.Status)),
                ("t.transferlocation", "TransferLocation"))
            .From("transaction t")
            .LeftJoin("purchaseorderstatus s", on:"s.id = t.status")
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
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "SourceSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("s.name", nameof(TransferOrderDataGridDTO.Status)),
                ("t.memo", nameof(TransferOrderDataGridDTO.Remarks)),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation")
                )
            .From("transaction t")
            .Join("transactionline tl", on:"tl.transaction = t.id")
            .LeftJoin("transferorderstatus s", on: "t.status = s.id")
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
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
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
                ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", "VendorName"),
                ("BUILTIN.DF(t.location)", "Location"),
                ("s.name", nameof(ReturnsDataGridDTO.Status)),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),

                ("t.memo", "Memo")
            )
            .From("transaction t")
            .LeftJoin("transferorderstatus s", on: "s.id = t.status")
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
                ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", "Vendor"),
                ("BUILTIN.DF(tl.location)", "FromWarehouse"),
                ("BUILTIN.DF(t.transferlocation)", "ToWarehouse"),
                ("t.custbody_dbti_prepared_by", "PreparedBy")
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline = 'T'")
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
            .LeftJoin("customrecord_dbti_vendor_bin_assignment vba", on: "t.entity = vba.custrecord_dbti_vba_vendor")
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
                ("tl.custcol_dbti_record_weight", nameof(ItemReceiptLineDTO.WeightTotal)),
                ("tl.custcol_dbti_actual_weight", nameof(ItemReceiptLineDTO.WeightReceived)),
                ("(tl.quantity / uom.conversionrate)", "QuantityPlanned"),
                ("(tl.quantity - tl.quantityshiprecv) / uom.conversionrate", "QuantityOpen"),
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
        var uri = dto.SourceType switch
        {
            ItemReceiptDTO.SourceTypes.PurchaseOrder => $"{netsuiteService.GetRestAPIURI}/record/v1/purchaseOrder/{dto.SourceInternalId}/!transform/itemReceipt",
            _ => $"{netsuiteService.GetRestletURI}?script=1853&deploy=1"
        };
        
        (string goodPayload, string badPayload) = dto.SourceType switch
        {
            ItemReceiptDTO.SourceTypes.PurchaseOrder => (CreatePOJson(dto, true), CreatePOJson(dto, false)),
            ItemReceiptDTO.SourceTypes.TransferOrder => (CreateTOJson(dto, true),CreateTOJson(dto, false)),
            _ => (CreateReturnsJson(dto, true), CreateReturnsJson(dto, false))
        };

        List<Exception> exceptions = [];

        try
        {
            _ = dto.SourceType.Equals(ItemReceiptDTO.SourceTypes.TransferOrder) ?
                await netsuiteService.MakeRequestOAuth1<object>(uri, goodPayload) :
                await netsuiteService.MakeRequest<object>(uri, goodPayload, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            if (!ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
                exceptions.Add(new Exception("Error posting good items: " + ex.Message));
        }

        try
        {
            _ = dto.SourceType.Equals(ItemReceiptDTO.SourceTypes.TransferOrder) ?
                await netsuiteService.MakeRequestOAuth1<object>(uri, badPayload) :
                await netsuiteService.MakeRequest<object>(uri, badPayload, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            if (!ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
                exceptions.Add(new Exception("Error posting bad items: " + ex.Message));
        }


        if (exceptions.Count > 0) throw new Exception(string.Join("\n\n", exceptions.Select(ex => ex.Message)));
        return true;
    }

    private string CreateTOJson(ItemReceiptDTO dto, bool isGood)
    {
        var obj = new
        {
            transferOrderId = dto.SourceInternalId,
            transferCategory = isGood ? 1 : 2,
            lines = dto.Lines.Where(x => x.QuantityGood > 0).Select(line =>
            {
                return new
                {
                    orderLine = line.LineNumber,
                    quantity = isGood ? line.QuantityGood : line.QuantityBad,
                    //rate = isGood ? (decimal?) null : 0,
                    inventoryDetail = new[]
                    {
                        new
                        {
                            inventoryStatus = isGood ? 1 : 2,
                            quantity = isGood ? line.QuantityGood : line.QuantityBad
                        }
                    }
                };
            })
        };
        return JsonSerializer.Serialize(obj, JSON_OPTS);
    }

    private string CreateReturnsJson(ItemReceiptDTO dto, bool isGood) => CreateTOJson(dto, isGood);

    private string CreatePOJson(ItemReceiptDTO dto, bool isGood)
    {
        var obj = new
        {
            custbody_dbti_receiving_category = isGood ? 1 : 2,
            item = new
            {
                items = dto.Lines.Where(line => line.QuantityPlanned != line.QuantityReceived).Select(line =>
                {
                    decimal lineQuantity = isGood ? line.QuantityGood : line.QuantityBad;
                    bool isItemReceived = line.IsReceived && lineQuantity > 0;
                    string? preferredBin = line.IsLocationBinUsed ? (isGood ? (dto.VendorPrefferedBin != 0 ? $"{dto.VendorPrefferedBin}" : $"{line.PrefferedBinAssignmentId}") : "5") : null;
                    return new
                    {
                        itemreceive = isItemReceived,
                        orderLine = line.LineNumber,
                        quantity = isItemReceived ? lineQuantity : (decimal?)null,
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
                                        inventoryStatus = isGood ? "1" : "2",
                                        binNumber = isGood ? preferredBin : "5",
                                        quantity = lineQuantity
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