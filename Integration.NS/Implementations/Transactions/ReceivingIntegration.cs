using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.NS.DataTransferObjects.ItemReceipt;
using Integration.NS.DataTransferObjects.Receiving;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Shared.Libraries.ViewModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.NS.Implementations.Transactions;

public class ReceivingIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContext,
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
                ("(tl.quantity / NVL(uom.conversionrate, 1))", nameof(PurchaseOrderLineDTO.QuantityPlanned))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .Join("item", on: "item.id = tl.item")
            .LeftJoin("unitstypeuom uom", on: "uom.internalid = tl.units")
            .WithFilters(
                Equal("t.tranid", docEntry),
                Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO>(query.Query);

        return response.items;
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("DateLastModified"));

        var builder = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DateLastModified"),
                ("t.location", "Location"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DeliveryDate"),
                ("t.memo", "Memo"),
                ("BUILTIN.DF(t.entity)", "VendorName"),
                ("s.name", nameof(PurchaseOrderDataGridDTO.Status)),
                ("t.transferlocation", "TransferLocation"))
            .From("transaction t")
            .LeftJoin("purchaseorderstatus s", on: "s.id = t.status")
            .WithDatagridIntent(intent)
            .WithFilters(
                Equal("t.recordtype", "purchaseorder"),
                In("t.status", new string[] { "B", "E" })
            )
            .WithSubsidiaries(httpContext, "t");

        SuiteQLQuery query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderDataGridDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<TransferOrderDataGridDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("DateLastModified"));

        List<int> allowedSubsidiaries = [];
        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue is not null)
        {
            allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];
        }

        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DateLastModified"),
                ("BUILTIN.DF(t.subsidiary)", "SourceSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"),
                ("BUILTIN.DF(tl.location)", "Location"),
                ("s.name", nameof(TransferOrderDataGridDTO.Status)),
                ("t.memo", nameof(TransferOrderDataGridDTO.Remarks)),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation")
                )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("transferorderstatus s", on: "t.status = s.id")
            .WithFilters(
                Equal("tl.mainline", "T"),
                NotEqual("t.custbody_dbti_transfer_category", 4), // returns - bad items
                NotEqual("t.custbody_dbti_transfer_category", 3), // returns - good items
                In("t.status", new string[] { "F", "E" }),
                Any(
                    All(
                        Equal("t.recordtype", "intercompanytransferorder"),
                        In("t.tosubsidiary", allowedSubsidiaries)
                    ),
                    All(
                        Equal("t.recordtype", "transferorder"),
                        In("t.subsidiary", allowedSubsidiaries)
                    )
                )
            )
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
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", "PreparedBy")
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(
                In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }),
                Equal("tl.mainline", "T"),
                Equal("t.tranid", docEntry),
                NotEqual("t.custbody_dbti_transfer_category", 3),
                NotEqual("t.custbody_dbti_transfer_category", 4),
                In("t.status", new string[] { "F", "E" })
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
                ("BUILTIN.DF(tl.location)", "DestinationLocation"),
                ("item.displayname", "ItemDescription"),
                ("(tl.quantity / NVL(uom.conversionrate, 1))", "QuantityPlanned"),
                ("(tl.quantityshiprecv / NVL(uom.conversionrate, 1))", "QuantityReceived"),
                ("((tl.quantity - tl.quantityshiprecv) / NVL(uom.conversionrate, 1))", "QuantityOpen"))
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .LeftJoin("unitstypeuom uom", on: "uom.internalid = tl.units")
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

        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("DateLastModified"));

        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DateLastModified"),
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
            .WithSubsidiaries(httpContext, "t", true)
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
                ("t.id", nameof(ReturnsDTO.Id)),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("BUILTIN.DF(t.subsidiary)", "FromSubsidiary"),
                ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", "Vendor"),
                ("BUILTIN.DF(tl.location)", "FromWarehouse"),
                ("BUILTIN.DF(t.transferlocation)", "ToWarehouse"),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", "PreparedBy")
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline = 'T'")
            .LeftJoin("employee e", "e.id = t.custbody_dbti_prepared_by")
            .WithSubsidiaries(httpContext, "t", true)
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
                ("(tl.quantity / NVL(uom.conversionrate, 1))", "QuantityPlanned")
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .LeftJoin("unitstypeuom uom", on: "tl.units = uom.internalid")
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
                ("t.location", nameof(ItemReceiptNSDTO.LocationId)),
                ("BUILTIN.DF(t.location)", nameof(ItemReceiptNSDTO.LocationName)),
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
            .Join("subsidiary s", on: "t.subsidiary = s.id")
            .WithFilters(
                In("t.type", new string[] { "TrnfrOrd", "PurchOrd" }),
                Equal("t.tranid", docEntry)
            ).Build();



        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemReceiptNSDTO>(query.Query);

        var result = response.items.FirstOrDefault();
        if (result is null) return null;



        return result.Adapt(new ItemReceiptDTO()
        {
            Location = new()
            {
                Id = result.LocationId,
                Name = result.LocationName
            }
        });
    }

    const string IF_RECEIPTS_QUERY = """
                SELECT
        			sum(rtl.quantity) / uom.conversionrate
        		FROM
        			previoustransactionlinelink pttl
        			JOIN transactionline rtl ON pttl.nextline = rtl.id
        			AND pttl.nextdoc = rtl.transaction
        		WHERE
        			pttl.previousdoc = tl.transaction
        			AND pttl.previousline = tl.id
        			AND pttl.nexttype = 'ItemRcpt'
        """;

    const string IF_TO_LINE_ID= """
            SELECT
                ttl.id
        	FROM
        		previoustransactionlinelink tolink
            JOIN 
                transactionline shippingline ON shippingline.id = tolink.previousline and shippingline.transaction = tolink.previousdoc
            JOIN 
                transactionline ttl on ttl.transferorderitemlineid = shippingline.transferorderitemlineid and ttl.transaction = shippingline.transaction
        	WHERE
        		tolink.nextdoc = tl.transaction
                AND ttl.transactionlinetype = 'RECEIVING'
        		AND tolink.nextline = tl.id
        		AND tolink.previoustype = 'TrnfrOrd'
        """;

    public async Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptItemFulfillmentLinesAsync(string docEntry)
    {

        var builder = builderFactory.Create()
            .Select(
                ("item.itemid", "ItemCode"),
                ("item.id", "ItemId"),
                ($"({IF_TO_LINE_ID})", "LineNumber"),
                ("uom.unitname", "UoM"),
                ("uom.conversionrate", "UoMRate"),
                ("loc.name", nameof(ItemReceiptLineDTO.LocationName)),
                ("loc.id", nameof(ItemReceiptLineDTO.LocationId)),
                ("loc.usebins", "LocationUsesBins"),
                ("item.displayname", "ItemDescription"),
                ("pb.bin", "PrefferedBinAssignmentId"),
                ("item.weight", nameof(ItemReceiptLineDTO.WeightPerItem)),
                ("tl.custcol_dbti_actual_weight", nameof(ItemReceiptLineDTO.WeightActual)),
                ("ABS(tl.quantity / uom.conversionrate)", "QuantityPlanned"),
                ($"({IF_RECEIPTS_QUERY})", "QuantityReceived")
            )
            .From("transactionline tl")
            .Join("item", on: "tl.item = item.id")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("location loc", on: "tl.location = loc.id")
            .Join("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin("(SELECT ibq.bin, ibq.item, b.location FROM itembinquantity ibq JOIN bin b ON ibq.bin = b.id WHERE preferredbin = \'T\') pb", on: "pb.item = item.id AND pb.location = tl.location")
            .WithFilters(
                Equal("t.tranid", docEntry)
            );

        var query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemReceiptLineDTO>(query.Query);
        return [.. response.items];
    }


    public async Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptLinesAsync(string docEntry, bool transferorder = false)
    {

        var builder = builderFactory.Create()
            .Select(
                ("item.itemid", "ItemCode"),
                ("item.id", "ItemId"),
                ("tl.id", "LineNumber"),
                ("uom.unitname", "UoM"),
                ("uom.conversionrate", "UoMRate"),
                ("loc.name", nameof(ItemReceiptLineDTO.LocationName)),
                ("loc.id", nameof(ItemReceiptLineDTO.LocationId)),
                ("loc.usebins", "LocationUsesBins"),
                ("item.displayname", "ItemDescription"),
                ("pb.bin", "PrefferedBinAssignmentId"),
                ("item.weight", nameof(ItemReceiptLineDTO.WeightPerItem)),
                ("tl.custcol_dbti_actual_weight", nameof(ItemReceiptLineDTO.WeightActual)),
                ("ABS(tl.quantity / uom.conversionrate)", "QuantityPlanned"),
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

    const int INVENTORY_STATUS_ID_GOOD = 1;
    const int INVENTORY_STATUS_ID_BAD = 3;

    public async Task<bool> PostItemReceipt(ItemReceiptDTO dto)
    {
        var uri = dto.SourceType == ItemReceiptDTO.SourceTypes.PurchaseOrder ?
            $"{netsuiteService.GetRestAPIURI}/record/v1/purchaseOrder/{dto.SourceInternalId}/!transform/itemReceipt" :
            $"{netsuiteService.GetRestletURI}?script=1853&deploy=1";

        (string goodPayload, string badPayload) = dto.SourceType switch
        {
            ItemReceiptDTO.SourceTypes.PurchaseOrder => (CreatePOJson(dto, true), CreatePOJson(dto, false)),
            ItemReceiptDTO.SourceTypes.TransferOrder => (CreateTOJson(dto, true), CreateTOJson(dto, false)),
            _ => (CreateReturnsJson(dto, true), CreateReturnsJson(dto, false))
        };

        List<Exception> exceptions = [];

        try
        {

            var hasGoodLines = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Status?.Id.Equals(INVENTORY_STATUS_ID_GOOD) ?? false));
            var hasBadLines = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Status?.Id.Equals(INVENTORY_STATUS_ID_BAD) ?? false));
            List<Task> tasks = [];

            if (dto.SourceType.Equals(ItemReceiptDTO.SourceTypes.PurchaseOrder))
            {
                if (hasGoodLines) tasks.Add(netsuiteService.MakeRequest<object>(uri, goodPayload, HttpMethod.Post));
                if (hasBadLines) tasks.Add(netsuiteService.MakeRequest<object>(uri, badPayload, HttpMethod.Post));

            }
            else
            {
                if (hasGoodLines) tasks.Add(netsuiteService.MakeRequestOAuth1<object>(uri, goodPayload));
                if (hasBadLines) tasks.Add(netsuiteService.MakeRequestOAuth1<object>(uri, badPayload));
            }

            await Task.WhenAll(tasks);

        }
        catch (Exception ex)
        {
            if (!ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
                exceptions.Add(new Exception("Error posting items: " + ex.Message));
        }

        if (exceptions.Count > 0) throw new Exception(string.Join("\n\n", exceptions.Select(ex => ex.Message)));
        return true;
    }

    public async Task<BarcodeDTO?> GetBarcodeData(string barcode)
    {
        var builder = builderFactory.Create()
            .Select(
                ("b.name", nameof(BarcodeNSDTO.Barcode)),
                ("b.custrecord_bpu_item", nameof(BarcodeNSDTO.ItemId)),
                ("BUILTIN.DF(b.custrecord_bpu_item)", nameof(BarcodeNSDTO.ItemName)),
                ("uom.internalid", nameof(BarcodeNSDTO.UoMId)),
                ("uom.unitName", nameof(BarcodeNSDTO.UoMName)),
                ("uom.conversionRate", nameof(BarcodeNSDTO.UoMRate))
            )
            .From("CUSTOMRECORD_BARCODE_PER_UOM b")
            .Join("unitstypeuom uom", on: "b.custrecord_bpu_uom = uom.internalid")
            .WithFilter(
                DataGridFilterUtilities.Equal("b.name", barcode)
            );

        var response = await netsuiteService.ExecuteSuiteQLQuery<BarcodeNSDTO>(builder.Build().Query);
        var barcodeData = response.items.FirstOrDefault();
        if (barcodeData == null) return null;

        return barcodeData.Adapt(new BarcodeDTO()
        {
            Item = new()
            {
                Id = barcodeData.ItemId,
                Name = barcodeData.ItemName,
            },
            UoM = new()
            {
                Id = barcodeData.UoMId,
                Name = barcodeData.UoMName,
                ConversionRate = barcodeData.UoMRate,
            }
        });
    }


    public async Task<(IEnumerable<ItemFulfillmentDTO>, int)> GetSTRItemFulfillments(int strId, DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(ItemFulfillmentDTO.DateLastModified)));

        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(ItemFulfillmentDTO.Id)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ItemFulfillmentDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ItemFulfillmentDTO.DateLastModified)),
                ("t.tranid", nameof(ItemFulfillmentDTO.ReferenceNumber)),
                ("s.name", nameof(ItemFulfillmentDTO.Status)),
                ("CONCAT(e.firstname, CONCAT(' ', e.lastname))", nameof(ItemFulfillmentDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("itemfulfillmentstatus s", on: "t.status = s.id")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(
                Equal("t.recordtype", "itemfulfillment"),
                Equal("t.status", "C"),
                Equal("ml.createdfrom", strId)
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ItemFulfillmentDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }

    public async Task<IEnumerable<ItemFulfillmentLineDTO>> GetItemFulfillmentLines(int ifId, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("tl.id", nameof(ItemFulfillmentLineDTO.LineNumber)),
                ("item.itemid", nameof(ItemFulfillmentLineDTO.ItemCode)),
                ("tl.item", nameof(ItemFulfillmentLineDTO.ItemId)),
                ("t.id", nameof(ItemFulfillmentLineDTO.ItemFullfillmentId)),
                ("tl.quantity", nameof(ItemFulfillmentLineDTO.QuantityOpen))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "t.id = tl.transaction")
            .Join("item", on: "tl.item = item.id")
            .WithFilters(
                Equal("t.recordtype", "itemfulfillment"),
                Equal("tl.transaction", ifId)
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ItemFulfillmentLineDTO>(query.Query);
        return response.items;
    }
    public async Task<(IEnumerable<ItemReceiptDataGridDTO> data, int count)> GetItemReceiptsDatagrid(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(ItemReceiptDataGridDTO.DateLastModified)));
        
        var query = builderFactory.Create()
            .Select(
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ItemReceiptDataGridDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ItemReceiptDataGridDTO.DateLastModified)),
                ("t.id", nameof(ItemReceiptDataGridDTO.Id)),
                ("t.tranid", nameof(ItemReceiptDataGridDTO.ReferenceNumber)),
                ("categ.name", nameof(ItemReceiptDataGridDTO.TransferCategory)),
                ("tcf.tranid", nameof(ItemReceiptDataGridDTO.CreatedFrom)),
                ("BUILTIN.DF(tl.location)", nameof(ItemReceiptDataGridDTO.FromLocation)),
                ("BUILTIN.DF(t.transferlocation)", nameof(ItemReceiptDataGridDTO.ToLocation))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline = 'T'")
            .LeftJoin("CUSTOMLIST_DBTI_TRANSFER_CATEGORY_LIST categ", "t.custbody_dbti_transfer_category = categ.id")
            .LeftJoin("transaction tcf", "tl.createdfrom = tcf.id")
            .WithFilters(
                Equal("t.recordtype", "itemreceipt")
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ItemReceiptDataGridDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }


    private string CreateReturnsJson(ItemReceiptDTO dto, bool isGood) => CreateTOJson(dto, isGood);
    private string CreateTOJson(ItemReceiptDTO dto, bool isGood)
    {
        int statusId = isGood ? INVENTORY_STATUS_ID_GOOD : INVENTORY_STATUS_ID_BAD;
        var lines = dto.Lines.Where(x => x.InventoryDetails.Any(y => y.Status?.Id == statusId));

        var obj = new
        {
            transferOrderId = dto.SourceInternalId,
            transferCategory = isGood ? 1 : 2,
            custbody_dbti_prepared_by = dto.PreparedById,
            receiverEmployeeId = dto.PreparedById,
            fulfillmentId = dto.ItemFulfillmentId,
            lines = lines.Where(x => x.QuantityAlloted > 0).Select(line =>
            {
                return new
                {
                    orderLine = line.LineNumber,
                    quantity = line.InventoryDetails.Sum(x => x.Status?.Id == statusId ? x.QuantityAlloted : 0),
                    inventoryDetail = line.InventoryDetails.Where(x => x.Status?.Id == statusId).Select(y => new
                    {
                        binNumber = y.Bin?.BinNumber,
                        inventoryStatus = statusId,
                        quantity = y.QuantityAlloted
                    })
                };
            })
        };
        return JsonSerializer.Serialize(obj, JSON_OPTS);
    }
    private string CreatePOJson(ItemReceiptDTO dto, bool isGood)
    {
        int statusId = isGood ? INVENTORY_STATUS_ID_GOOD : INVENTORY_STATUS_ID_BAD;
        var lines = dto.Lines.Where(x => x.InventoryDetails.Any(y => y.Status?.Id == statusId));

        var obj = new
        {
            defaultValues = new {
                itemfulfillment = dto.ItemFulfillmentId
            },
            custbody_dbti_receiving_category = isGood ? 1 : 2,
            custbody_dbti_prepared_by = dto.PreparedById,
            custbody_dbti_received_by = dto.PreparedById,
            item = new
            {
                items = lines.Where(line => line.QuantityPlanned != line.QuantityReceived).Select(line =>
                {
                    decimal lineQuantity = line.InventoryDetails.Sum(x => x.Status?.Id == statusId ? x.QuantityAlloted : 0);
                    bool isItemReceived = line.IsReceived && lineQuantity > 0;
                    return new
                    {
                        itemreceive = isItemReceived,
                        orderLine = line.LineNumber,
                        quantity = isItemReceived ? lineQuantity : (decimal?)null,
                        custcol_dbti_actual_weight = isItemReceived ? line.WeightActual : (decimal?)null,
                        rate = isGood ? (decimal?)null : 0,
                        inventoryDetail = isItemReceived ? new
                        {
                            inventoryAssignment = new
                            {
                                items = line.InventoryDetails.Where(x => x.Status?.Id == statusId).Select(x =>
                                new 
                                {
                                    inventoryStatus = statusId,
                                    binNumber = x.Bin?.Id,
                                    quantity = lineQuantity
                                })
                            }
                        } : null,
                        location = line.LocationId
                    };
                })
            },
            memo = "Created via WMS"
        };

        return JsonSerializer.Serialize(obj, JSON_OPTS);
    }

    public async Task<(IEnumerable<PurchaseOrderStatusDTO>, int)> GetPurchaseOrderStatuses(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("s.name", nameof(PurchaseOrderStatusDTO.Name)),
                ("s.id", nameof(PurchaseOrderStatusDTO.Id))
            )
            .From("purchaseorderstatus s")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<PurchaseOrderStatusDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<TransferOrderStatusDTO>, int)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("s.name", nameof(TransferOrderStatusDTO.Name)),
                ("s.id", nameof(TransferOrderStatusDTO.Id))
            )
            .From("transferorderstatus s")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<TransferOrderStatusDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    readonly JsonSerializerOptions JSON_OPTS = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}