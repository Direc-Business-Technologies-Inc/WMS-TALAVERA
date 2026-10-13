using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.NS.DataTransferObjects.ItemReceipt;
using Integration.NS.DataTransferObjects.Others;
using Integration.NS.DataTransferObjects.Receiving;
using Application.DataTransferObjects.Others.NS;
using Integration.NS.Exceptions;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
// this class sucks
public class ReceivingIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContext,
    SuiteQLQueryBuilderFactoryService builderFactory,
    ILogger<ReceivingIntegration> logger)
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
        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("ReferenceNumber"));

        int[] AllowedLocations = [-1];
        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedLocations")?.Value;
        if (claimValue is not null)
        {
            AllowedLocations = JsonSerializer.Deserialize<int[]>(claimValue) ?? [-1];
        }

        var builder = builderFactory.Create()
            .Distinct()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DateLastModified"),
                ("t.location", "Location"),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DeliveryDate"),
                ("t.memo", "Memo"),
                ("t.location", "LocationId"),
                ("BUILTIN.DF(t.entity)", "VendorName"),
                ("s.name", nameof(PurchaseOrderDataGridDTO.Status)),
                ("t.transferlocation", "TransferLocation"))
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id")
            .LeftJoin("purchaseorderstatus s", on: "s.id = t.status")
            .WithDatagridIntent(intent)
            .WithFilters(
                Equal("t.recordtype", "purchaseorder"),
                In("t.status", new string[] { "B", "E" }),
                In("tl.location", AllowedLocations)
            )
            .WithSubsidiaries(httpContext, "t");

        SuiteQLQuery query = builder.Build();

        return await QueryListWithOffsetClampAsync(intent, "Purchase orders list", async skipOverride =>
        {
            if (skipOverride.HasValue) query.Offset = skipOverride.Value;

            var response = await query.ExecuteWithPaging<PurchaseOrderDataGridDTO>(netsuiteService);
            return response;
        });
    }

    public async Task<(IEnumerable<TransferOrderDataGridDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("ReferenceNumber"));

        List<int> allowedSubsidiaries = [];
        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue is not null)
        {
            allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];
        }

        int[] AllowedLocations = [-1];
        claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedLocations")?.Value;
        if (claimValue is not null)
        {
            AllowedLocations = JsonSerializer.Deserialize<int[]>(claimValue) ?? [-1];
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
                ),
                In("t.transferlocation", AllowedLocations)
            )
            .WithDatagridIntent(intent)
            .Build();

        return await QueryListWithOffsetClampAsync(intent, "Transfer orders list", async skipOverride =>
        {
            if (skipOverride.HasValue) query.Offset = skipOverride.Value;

            var response = await query.ExecuteWithPaging<TransferOrderDataGridDTO>(netsuiteService);
            return response;
        });
    }

    /// <summary>
    /// Runs a paged list query with a single offset-clamp retry. NetSuite
    /// rejects any offset past the end of its (possibly empty) result set, and
    /// persisted Radzen pager positions routinely request such offsets once a
    /// result set shrinks or empties. Falls back to the last legal page using
    /// NetSuite's "between 0 and N" hint, else the first page. A second
    /// OffsetOutOfBounds means the data moved under us; surface it rather than
    /// looping.
    /// </summary>
    private async Task<(IEnumerable<T> items, int totalResults)> QueryListWithOffsetClampAsync<T>(
        DataGridIntent intent,
        string gridName,
        Func<int?, Task<Application.DataTransferObjects.Others.NS.NetSuiteResponse<T>>> executeQuery)
    {
        try
        {
            var response = await executeQuery(null);
            return (response.items, response.totalResults);
        }
        catch (NetSuiteValidationException ex) when (ex.ErrorCode == NetSuiteErrorCode.OffsetOutOfBounds)
        {
            var take = intent.Take <= 0 ? 10 : intent.Take;
            var bound = NetSuiteErrorCodes.GetOffsetUpperBound(ex.Message, ex.Detail);
            var retrySkip = bound.HasValue
                ? Math.Max(0, (bound.Value / take) * take)
                : 0;

            if (retrySkip == intent.Skip)
            {
                throw;
            }

            logger.LogWarning(
                ex,
                "{GridName} requested offset {Skip} is out of bounds; retrying once at offset {RetrySkip}.",
                gridName, intent.Skip, retrySkip);

            var retry = await executeQuery(retrySkip);
            return (retry.items, retry.totalResults);
        }
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
        try
        {
            var response = await QueryTransferOrderLinesAsync(Id, intent);
            return (response.items, response.totalResults);
        }
        catch (NetSuiteValidationException ex) when (ex.ErrorCode == NetSuiteErrorCode.OffsetOutOfBounds)
        {
            // Radzen derives Skip from CurrentPage * PageSize, so the requested
            // offset can land past the end of the result set: changing the page
            // size on a later page, a stale persisted page index, or a
            // filter/sort that shrank the result set. Clamp to the last legal
            // page and retry once so the user sees rows instead of a 404.
            //
            // Preference order: the N from NetSuite's "between 0 and N" hint
            // (last legal start = largest multiple of Take within N), else the
            // first page. Single retry only — a second OffsetOutOfBounds means
            // the data moved under us; surface it rather than looping.
            var take = intent.Take <= 0 ? 10 : intent.Take;
            var bound = NetSuiteErrorCodes.GetOffsetUpperBound(ex.Message, ex.Detail);
            var retrySkip = bound.HasValue
                ? Math.Max(0, (bound.Value / take) * take)
                : 0;

            if (retrySkip == intent.Skip)
            {
                throw;
            }

            logger.LogWarning(
                ex,
                "Transfer order {TransferOrderId} lines requested offset {Skip} is out of bounds; retrying once at offset {RetrySkip}.",
                Id, intent.Skip, retrySkip);

            var retry = await QueryTransferOrderLinesAsync(Id, intent, skipOverride: retrySkip);
            throw new NetSuiteOffsetCorrectedException<IEnumerable<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>>(retry.items, retry.totalResults, retrySkip, ex);
        }
    }

    /// <summary>
    /// Builds the TO-lines SuiteQL query from <paramref name="intent"/> and runs
    /// it. The query text is rebuilt per attempt (rather than mutating
    /// <paramref name="intent"/>) so the caller's grid state is never rewritten
    /// behind Radzen's back.
    /// </summary>
    private async Task<Application.DataTransferObjects.Others.NS.NetSuiteResponse<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>> QueryTransferOrderLinesAsync(string Id, DataGridIntent intent, int? skipOverride = null)
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

        if (skipOverride.HasValue)
        {
            query.Offset = skipOverride.Value;
        }

        return await netsuiteService.ExecuteSuiteQLQuery<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>(query.Query, limit: query.Limit, offset: query.Offset);
    }

    public async Task<(IEnumerable<ReturnsDataGridDTO>, int count)> GetReturnsListAsync(DataGridIntent intent)
    {
        return await QueryListWithOffsetClampAsync(intent, "Returns list", async skipOverride =>
        {
            var response = await QueryReturnsListAsync(intent, skipOverride);
            return response;
        });
    }

    private async Task<Application.DataTransferObjects.Others.NS.NetSuiteResponse<ReturnsDataGridDTO>> QueryReturnsListAsync(DataGridIntent intent, int? skipOverride = null)
    {
        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending("ReferenceNumber"));

        int[] AllowedLocations = [-1];
        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedLocations")?.Value;
        if (claimValue is not null)
        {
            AllowedLocations = JsonSerializer.Deserialize<int[]>(claimValue) ?? [-1];
        }

        List<AppFilterDescriptor> filters =
        [
            In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
            In("t.status", new string[] { "F", "E" }),
            Any(
                Equal("t.custbody_dbti_transfer_category", 3),
                Equal("t.custbody_dbti_transfer_category", 4)),
            In("t.transferlocation", AllowedLocations)
        ];

        var subsidiaryScope = ReturnSubsidiaryScope();
        if (subsidiaryScope is not null) filters.Add(subsidiaryScope);

        var query = builderFactory.Create()
            .Select(
                ("t.id", "Id"),
                ("t.tranid", "ReferenceNumber"),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "DateLastModified"),
                ("BUILTIN.DF(t.subsidiary)", "SourceSubsidiary"),
                ("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"),
                ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", "VendorName"),
                ("BUILTIN.DF(ml.location)", "Location"),
                ("s.name", nameof(ReturnsDataGridDTO.Status)),
                ("BUILTIN.DF(t.transferlocation)", "TransferLocation"),
                ("t.memo", "Memo")
            )
            .From("transaction t")
            .LeftJoin("transferorderstatus s", on: "s.id = t.status")
            .LeftJoin("transactionline ml", "ml.transaction = t.id AND ml.mainline = 'T'")
            .WithDatagridIntent(intent)
            .WithFilters([.. filters]).Build();

        if (skipOverride.HasValue)
        {
            query.Offset = skipOverride.Value;
        }

        var response = await query.ExecuteWithPaging<ReturnsDataGridDTO>(netsuiteService);
        return response;
    }

    public async Task<ReturnsDTO?> GetReturnsHeaderAsync(string docEntry)
    {
        List<AppFilterDescriptor> filters =
        [
            In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
            Any(
                Equal("t.custbody_dbti_transfer_category", 3),
                Equal("t.custbody_dbti_transfer_category", 4)),
            Equal("t.tranid", docEntry)
        ];

        var subsidiaryScope = ReturnSubsidiaryScope();
        if (subsidiaryScope is not null) filters.Add(subsidiaryScope);

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
            .WithFilters([.. filters]).Build();

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
                In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
                Equal("tl.transactionlinetype", "RECEIVING"),
                Equal("t.tranid", docEntry),
                Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReturnsLineDTO>(query.Query);
        return [.. response.items];
    }

    private AppFilterDescriptor? ReturnSubsidiaryScope()
    {
        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue is null) return null;

        List<int> allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];
        if (allowedSubsidiaries.Count == 0) return null;

        return Any(
            All(
                Equal("t.recordtype", "intercompanytransferorder"),
                In("t.tosubsidiary", allowedSubsidiaries)
            ),
            All(
                Equal("t.recordtype", "transferorder"),
                In("t.subsidiary", allowedSubsidiaries)
            )
        );
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

    /// <summary>
    ///     Derived table mapping each Item Fulfillment line to its source TO
    ///     RECEIVING line id (<c>tolineid</c>) and that line's <c>quantityshiprecv</c>.
    ///     <para>
    ///     All correlation is resolved INSIDE the derived table; the outer join
    ///     predicate only correlates the table's own output columns (ifdoc/ifline)
    ///     to the driving alias. A previous shape embedded the same mapping as
    ///     correlated scalar subqueries in the SELECT list (the QuantityReceived one
    ///     nested two levels deep, reaching the outer <c>uom</c> alias), which
    ///     NetSuite rejects with <c>400 [INVALID_PARAMETER] ... Invalid or
    ///     unsupported search</c> — see .opencode/docs/SuiteQL-Shape-Rules.md.
    ///     </para>
    /// </summary>
    /// <returns>A parenthesised subquery. Append the join alias, e.g. $"{IF_TO_LINE_MAP} ifmap".</returns>
    const string IF_TO_LINE_MAP = """
            SELECT
                tolink.nextdoc AS ifdoc,
                tolink.nextline AS ifline,
                ttl.id AS tolineid,
                ttl.quantityshiprecv AS qtyrecv
        	FROM
        		previoustransactionlinelink tolink
            JOIN
                transactionline shippingline ON shippingline.id = tolink.previousline and shippingline.transaction = tolink.previousdoc
            JOIN
                transactionline ttl on ttl.transferorderitemlineid = shippingline.transferorderitemlineid and ttl.transaction = shippingline.transaction
        	WHERE
                ttl.transactionlinetype = 'RECEIVING'
        		AND tolink.previoustype = 'TrnfrOrd'
        """;

    public async Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptItemFulfillmentLinesAsync(string docEntry)
    {

        var builder = builderFactory.Create()
            .Select(
                ("item.itemid", "ItemCode"),
                ("item.id", "ItemId"),
                ("ifmap.tolineid", "LineNumber"),
                ("uom.unitname", "UoM"),
                ("uom.conversionrate", "UoMRate"),
                ("loc.name", nameof(ItemReceiptLineDTO.LocationName)),
                ("loc.id", nameof(ItemReceiptLineDTO.LocationId)),
                ("barcode.name", nameof(ItemReceiptLineDTO.ItemBarcode)),
                ("loc.usebins", "LocationUsesBins"),
                ("item.displayname", "ItemDescription"),
                ("pb.bin", "PrefferedBinAssignmentId"),
                ("item.weight", nameof(ItemReceiptLineDTO.WeightPerItem)),
                ("tl.custcol_dbti_actual_weight", nameof(ItemReceiptLineDTO.WeightActual)),
                ("ABS(tl.quantity / uom.conversionrate)", "QuantityPlanned"),
                ("item.usebins", "ItemUseBins"),
                ("NVL(ifmap.qtyrecv / uom.conversionrate, 0)", "QuantityReceived")
            )
            .From("transactionline tl")
            .Join("item", on: "tl.item = item.id")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("location loc", on: "tl.location = loc.id")
            .Join("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin($"({IF_TO_LINE_MAP}) ifmap", "ifmap.ifdoc = tl.transaction AND ifmap.ifline = tl.id")
            .LeftJoin("CUSTOMRECORD_BARCODE_PER_UOM barcode", "barcode.custrecord_bpu_uom = tl.units AND barcode.custrecord_bpu_item = tl.item")
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
                ("item.usebins", nameof(ItemReceiptLineDTO.ItemUseBins)),
                ("tl.id", "LineNumber"),
                ("uom.unitname", "UoM"),
                ("uom.conversionrate", "UoMRate"),
                ("loc.name", nameof(ItemReceiptLineDTO.LocationName)),
                ("loc.id", nameof(ItemReceiptLineDTO.LocationId)),
                ("loc.usebins", "LocationUsesBins"),
                ("item.displayname", "ItemDescription"),
                ("barcode.name", nameof(ItemReceiptLineDTO.ItemBarcode)),
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
            .LeftJoin("CUSTOMRECORD_BARCODE_PER_UOM barcode", "barcode.custrecord_bpu_uom = tl.units AND barcode.custrecord_bpu_item = tl.item")
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

    /// <summary>
    /// Per-receiving-line planned vs received quantities of the source Transfer Order,
    /// read from the TO ledger (<c>quantityshiprecv</c>) rather than from Item Receipts
    /// linked to an Item Fulfillment. Receipts posted from mobile are linked TO→IR, so an
    /// IF-based sum cannot see them; the TO counter can. Keyed by TO line id, which is what
    /// <c>ItemReceiptLineDTO.LineNumber</c> already carries on both the IF and the
    /// non-IF load paths.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, TransferOrderLineRemaining>> GetTransferOrderLineRemainingAsync(int sourceInternalId)
    {
        var query = builderFactory.Create()
            .Select(
                ("tl.id", "LineId"),
                ("item.itemid", "ItemCode"),
                ("item.displayname", "ItemDescription"),
                ("ABS(tl.quantity / NVL(uom.conversionrate, 1))", "QuantityPlanned"),
                ("NVL(tl.quantityshiprecv / NVL(uom.conversionrate, 1), 0)", "QuantityReceived")
            )
            .From("transactionline tl")
            .Join("item", on: "tl.item = item.id")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("unitstypeuom uom", on: "tl.units = uom.internalid")
            .WithFilters(
                Equal("t.id", sourceInternalId),
                In("t.recordtype", new[] { "transferorder", "intercompanytransferorder" }),
                Equal("tl.mainline", "F"),
                Equal("tl.transactionlinetype", "RECEIVING")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TransferOrderLineRemaining>(query.Query);

        return response.items.ToDictionary(x => x.LineId);
    }

    private sealed record TransferOrderLineRemaining
    {
        public int LineId { get; init; }
        public string ItemCode { get; init; } = string.Empty;
        public string ItemDescription { get; init; } = string.Empty;
        public decimal QuantityPlanned { get; init; }
        public decimal QuantityReceived { get; init; }
        public decimal QuantityRemaining => QuantityPlanned - QuantityReceived;
    }

    const int INVENTORY_STATUS_ID_GOOD = 1;
    const int INVENTORY_STATUS_ID_BAD = 3;
    const int INVENTORY_STATUS_ID_MISSING = 6;

    public async Task<bool> PostItemReceipt(ItemReceiptDTO dto)
    {
        var uri = dto.SourceType == ItemReceiptDTO.SourceTypes.PurchaseOrder ?
            $"{netsuiteService.GetRestAPIURI}/record/v1/purchaseOrder/{dto.SourceInternalId}/!transform/itemReceipt" :
            $"{netsuiteService.GetRestletURI}?script=1853&deploy=1";

        List<Exception> exceptions = [];
        List<Task> tasks = [];

        try
        {
            // A line without a resolved NetSuite line number cannot be posted:
            // orderLine: 0 is always rejected with a 400 that surfaces only a
            // terse "(0)" detail. Fail fast with a message identifying the line.
            var unnumberedLine = dto.Lines.FirstOrDefault(x => x.LineNumber <= 0);

            if (unnumberedLine is not null)
            {
                throw new Exception(
                    $"Line '{unnumberedLine.ItemCode}' ({unnumberedLine.ItemDescription}) has no NetSuite " +
                    "line number. Reload the page and retry; if it persists, the source document's " +
                    "lines could not be matched to the receipt.");
            }

            // Over-receipt guard (non-PO only). The form's open quantity is a client-side
            // number and can be stale by the time the operator submits — most notably when
            // another device already received against the same Transfer Order, because such
            // receipts are linked TO→IR and invisible to the IF-based math. Re-read the TO
            // ledger and refuse anything beyond what is left, before any HTTP post, so the
            // failure is an actionable message instead of a terse NetSuite 400. Lines that
            // the lookup does not resolve are left to the orderLine guard above.
            if (dto.SourceType != ItemReceiptDTO.SourceTypes.PurchaseOrder)
            {
                var remainingByLineId = await GetTransferOrderLineRemainingAsync(dto.SourceInternalId);

                foreach (var line in dto.Lines.Where(x => x.InventoryDetails.Sum(y => y.QuantityAlloted) > 0))
                {
                    if (!remainingByLineId.TryGetValue(line.LineNumber, out var remaining))
                        continue;

                    var allotted = line.InventoryDetails.Sum(y => y.QuantityAlloted);

                    if (allotted <= remaining.QuantityRemaining)
                        continue;

                    logger.LogWarning(
                        "PostItemReceipt rejected over-receipt on line {LineId} ({ItemCode} {ItemDescription}): " +
                        "allotted {Allotted} exceeds remaining {Remaining} ({Received} already received, " +
                        "{SourceType} {SourceInternalId})",
                        line.LineNumber,
                        remaining.ItemCode,
                        remaining.ItemDescription,
                        allotted,
                        remaining.QuantityRemaining,
                        remaining.QuantityReceived,
                        dto.SourceType,
                        dto.SourceInternalId);

                    throw new Exception(
                        $"Line '{remaining.ItemCode}' ({remaining.ItemDescription}): allotted {allotted} " +
                        $"exceeds remaining {remaining.QuantityRemaining} ({remaining.QuantityReceived} already " +
                        "received, possibly from another device). Reload and adjust quantities.");
                }
            }

            // Determine which statuses actually exist in the receipt
            var hasGoodLines = dto.Lines.Any(x =>
                x.InventoryDetails.Any(y =>
                    y.Status?.Id == INVENTORY_STATUS_ID_GOOD));

            var hasBadLines = dto.Lines.Any(x =>
                x.InventoryDetails.Any(y =>
                    y.Status?.Id == INVENTORY_STATUS_ID_BAD));

            var hasMissingLines = dto.Lines.Any(x =>
                x.InventoryDetails.Any(y =>
                    y.Status?.Id == INVENTORY_STATUS_ID_MISSING));

            // Helper to create the correct payload for the source type
            string CreatePayload(int statusId)
            {
                return dto.SourceType switch
                {
                    ItemReceiptDTO.SourceTypes.PurchaseOrder =>
                        CreatePOJson(dto, statusId),

                    ItemReceiptDTO.SourceTypes.TransferOrder =>
                        CreateTOJson(dto, statusId),

                    _ =>
                        CreateReturnsJson(dto, statusId)
                };
            }

            void AddPostTask(bool hasLines, int statusId)
            {
                if (!hasLines)
                    return;

                var payload = CreatePayload(statusId);

                // G1: the entire payload (including the fulfillment id actually
                // sent) is logged for every post attempt, successful or not.
                logger.LogInformation(
                    "PostItemReceipt payload ({SourceType}, status {StatusId}) to {Uri}: {Payload}",
                    dto.SourceType,
                    statusId,
                    uri,
                    payload);

                tasks.Add(
                    netsuiteService.MakeRequest<object>(
                        uri,
                        payload,
                        HttpMethod.Post));
            }

            AddPostTask(hasBadLines, INVENTORY_STATUS_ID_BAD);
            AddPostTask(hasGoodLines, INVENTORY_STATUS_ID_GOOD);
            AddPostTask(hasMissingLines, INVENTORY_STATUS_ID_MISSING);

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Nothing was posted (e.g. payload construction threw before any
            // task was created): preserve the original exception instead of
            // swallowing it into a true return.
            if (tasks.Count == 0)
            {
                logger.LogWarning(
                    ex,
                    "PostItemReceipt aborted before posting ({SourceType}, uri: {Uri})",
                    dto.SourceType,
                    uri);

                throw;
            }

            foreach (var task in tasks.Where(t => t.IsFaulted))
            {
                foreach (var exception in task.Exception!.Flatten().InnerExceptions)
                {
                    // Each rewrap keeps the original exception as inner so the
                    // stack chain reaches the log; the unreachable
                    // "Empty response from NetSuite API" filter was removed (G5).
                    exceptions.Add(new Exception(
                        "Error posting items: " + exception.Message,
                        exception));
                }
            }

            // No faulted-task message could be captured; rethrow rather than
            // report success.
            if (exceptions.Count == 0)
            {
                logger.LogWarning(
                    ex,
                    "PostItemReceipt failed without faulted task detail ({SourceType}, uri: {Uri})",
                    dto.SourceType,
                    uri);

                throw;
            }
        }

        if (exceptions.Count > 0)
        {
            // The aggregate carries every collected exception as inner, so the
            // original stack chain is preserved while the toast keeps showing
            // the same joined message (G4/G6).
            var aggregateException = new AggregateException(
                string.Join(
                    "\n\n",
                    exceptions.Select(ex => ex.Message)),
                exceptions);

            logger.LogError(
                aggregateException,
                "PostItemReceipt failed for {SourceType} (uri: {Uri})",
                dto.SourceType,
                uri);

            throw aggregateException;
        }

        return true;

        //(string goodPayload, string badPayload) = dto.SourceType switch
        //{
        //    ItemReceiptDTO.SourceTypes.PurchaseOrder => (CreatePOJson(dto, true), CreatePOJson(dto, false)),
        //    ItemReceiptDTO.SourceTypes.TransferOrder => (CreateTOJson(dto, true), CreateTOJson(dto, false)),
        //    _ => (CreateReturnsJson(dto, true), CreateReturnsJson(dto, false))
        //};

        //List<Exception> exceptions = [];

        //try
        //{

        //    var hasGoodLines = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Status?.Id.Equals(INVENTORY_STATUS_ID_GOOD) ?? false));
        //    var hasBadLines = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Status?.Id.Equals(INVENTORY_STATUS_ID_BAD) ?? false));
        //    var hasMissingLines = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Status?.Id.Equals(INVENTORY_STATUS_ID_MISSING) ?? false));

        //    List<Task> tasks = [];

        //    if (dto.SourceType.Equals(ItemReceiptDTO.SourceTypes.PurchaseOrder))
        //    {
        //        if (hasGoodLines) tasks.Add(netsuiteService.MakeRequest<object>(uri, goodPayload, HttpMethod.Post));
        //        if (hasBadLines) tasks.Add(netsuiteService.MakeRequest<object>(uri, badPayload, HttpMethod.Post));

        //    }
        //    else
        //    {
        //        if (hasGoodLines) tasks.Add(netsuiteService.MakeRequestOAuth1<object>(uri, goodPayload));
        //        if (hasBadLines) tasks.Add(netsuiteService.MakeRequestOAuth1<object>(uri, badPayload));
        //    }

        //    foreach (Task x in tasks)
        //    {
        //        await x;
        //    }

        //}
        //catch (Exception ex)
        //{
        //    if (!ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        //        exceptions.Add(new Exception("Error posting items: " + ex.Message));
        //}

        //if (exceptions.Count > 0) throw new Exception(string.Join("\n\n", exceptions.Select(ex => ex.Message)));
        //return true;
    }

    public async Task<BarcodeDTO?> GetBarcodeData(string barcode, int? location)
    {
        // A location is optional. It scopes only the location-dependent facts: quantity,
        // preferred bin and preferred vendor.
        //
        // Scoped, the location rides in the JOIN condition so a barcode still resolves for
        // an item with no stock there. It must never ride in the WHERE clause: a predicate
        // on a LEFT JOINed table discards the row, which is why "loc.id = N" used to report
        // valid barcodes as not found.
        //
        // Unscoped, the location-dependent fields are dropped rather than guessed. Preferred
        // bin and vendor would otherwise span every location and subsidiary, fanning a
        // multi-location or multi-subsidiary item into duplicate rows and risking a bin or
        // vendor from the wrong scope.
        var scoped = location > 0;

        var builder = builderFactory.Create()
            .Select(
                ("b.name", nameof(BarcodeNSDTO.Barcode)),
                ("b.custrecord_bpu_item", nameof(BarcodeNSDTO.ItemId)),
                ("item.description", nameof(BarcodeNSDTO.ItemName)),
                ("item.itemId", nameof(BarcodeNSDTO.ItemCode)),
                ("item.weight", nameof(BarcodeNSDTO.ItemWeight)),
                ("uom.internalid", nameof(BarcodeNSDTO.UoMId)),
                ("uom.unitName", nameof(BarcodeNSDTO.UoMName)),
                ("uom.conversionRate", nameof(BarcodeNSDTO.UoMRate)),
                // Unscoped, summing keeps this to one row per barcode. Joining
                // aggregateitemlocation without a location yields one row per stocking
                // location instead.
                (scoped
                    ? "ail.quantityonhand"
                    : "(SELECT SUM(ail.quantityonhand) FROM aggregateitemlocation ail WHERE ail.item = item.id)",
                 nameof(BarcodeNSDTO.QuantityOnHand)),
                (scoped
                    ? "ail.quantityavailable"
                    : "(SELECT SUM(ail.quantityavailable) FROM aggregateitemlocation ail WHERE ail.item = item.id)",
                 nameof(BarcodeNSDTO.QuantityAvailable))
            )
            .From("CUSTOMRECORD_BARCODE_PER_UOM b")
            .Join("unitstypeuom uom", on: "b.custrecord_bpu_uom = uom.internalid")
            .Join("item", "item.id = b.custrecord_bpu_item")
            .WithFilters(
                Equal("b.name", barcode)
            );

        if (scoped)
        {
            builder = builder
                .LeftJoin("aggregateitemlocation ail", on: $"ail.item = item.id AND ail.location = {location}")
                .LeftJoin("location loc", on: "ail.location = loc.id")
                .LeftJoin($"{SuiteQLFragments.PreferredBin(location)} pb", on: "pb.item = item.id AND pb.location = loc.id")
                .LeftJoin($"{SuiteQLFragments.PreferredVendorForLocation(location)} pv", on: "pv.item = item.id")
                .Select(
                    ("pb.binnumber", nameof(BarcodeNSDTO.PreferredBin)),
                    ("pv.vendor", nameof(BarcodeNSDTO.PreferredVendorId)),
                    ("BUILTIN.DF(pv.vendor)", nameof(BarcodeNSDTO.PreferredVendorName))
                );
        }

        var response = await netsuiteService.ExecuteSuiteQLQuery<BarcodeNSDTO>(builder.Build().Query);
        var barcodeData = response.items.FirstOrDefault();
        if (barcodeData == null) return null;

        return barcodeData.Adapt(new BarcodeDTO()
        {
            Item = new()
            {
                Id = barcodeData.ItemId,
                Name = barcodeData.ItemName,
                ItemNumber = barcodeData.ItemCode,
                Weight = barcodeData.ItemWeight,
                QuantityAvailable = barcodeData.QuantityAvailable,
                QuantityOnHand = barcodeData.QuantityOnHand,
                PreferredBin = barcodeData.PreferredBin,
                PreferredVendorId = barcodeData.PreferredVendorId,
                PreferredVendorName = barcodeData.PreferredVendorName,
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
        intent = intent.Adapt<DataGridIntent>();
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
                ("BUILTIN.DF(t.entity)", nameof(ItemReceiptDataGridDTO.Vendor)),
                ("categ.name", nameof(ItemReceiptDataGridDTO.TransferCategory)),
                ("tcf.tranid", nameof(ItemReceiptDataGridDTO.CreatedFrom)),
                ("BUILTIN.DF(tl.location)", nameof(ItemReceiptDataGridDTO.ToLocation)), //swapped with FromLocation to match the correct field in the datagrid
                ("BUILTIN.DF(t.transferlocation)", nameof(ItemReceiptDataGridDTO.FromLocation)) 
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline = 'T'")
            .LeftJoin("CUSTOMLIST_DBTI_TRANSFER_CATEGORY_LIST categ", "t.custbody_dbti_transfer_category = categ.id")
            .LeftJoin("transaction tcf", "tl.createdfrom = tcf.id")
            .WithFilters(
                Equal("t.recordtype", "itemreceipt")
            )
            .WithFromOrToSubsidiaries(httpContext, "t")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ItemReceiptDataGridDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }


    private string CreateReturnsJson(ItemReceiptDTO dto, bool isGood) => CreateTOJson(dto, isGood);
    private string CreateReturnsJson(ItemReceiptDTO dto, int statusId) => CreateTOJson(dto, statusId);
    private string CreateTOJson(ItemReceiptDTO dto, bool isGood)
    {
        int statusId = isGood ? INVENTORY_STATUS_ID_GOOD : INVENTORY_STATUS_ID_BAD;
        var lines = dto.Lines.Where(x => x.InventoryDetails.Any(y => y.Status?.Id == statusId));

        var obj = new
        {
            transferOrderId = dto.SourceInternalId,
            transferCategory = isGood ? 1 : 2,
            custbody_dbti_created_in_wms = true,
            custbody_dbti_prepared_by = dto.PreparedById,
            receiverEmployeeId = dto.PreparedById,
            fulfillmentId = dto.ItemFulfillmentId,
            memo = dto.Remarks,
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
    private string CreateTOJson(ItemReceiptDTO dto, int statusId)
    {
        var lines = dto.Lines.Where(x => x.InventoryDetails.Any(y => y.Status?.Id == statusId));

        // Mirrors the restlet contract proven by the mobile path
        // (TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet):
        // no binNumber in inventoryDetail, rate 0 for bad items only, and
        // recWeight/actualWeight on every line.
        var obj = new
        {
            transferOrderId = dto.SourceInternalId,
            transferCategory = statusId switch
            {
                INVENTORY_STATUS_ID_GOOD => 1,
                INVENTORY_STATUS_ID_BAD => 3,
                INVENTORY_STATUS_ID_MISSING => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(statusId), statusId, "Unsupported inventory status for transfer-order receipt.")
            },
            custbody_dbti_created_in_wms = true,
            custbody_dbti_prepared_by = dto.PreparedById,
            receiverEmployeeId = dto.PreparedById,
            fulfillmentId = dto.ItemFulfillmentId,
            memo = dto.Remarks,
            lines = lines.Where(x => x.QuantityAlloted > 0).Select(line =>
            {
                var quantity = line.InventoryDetails.Sum(x => x.Status?.Id == statusId ? x.QuantityAlloted : 0);

                return new
                {
                    orderLine = line.LineNumber,
                    quantity,
                    recWeight = line.WeightPerItem * quantity,
                    actualWeight = line.WeightActual,
                    rate = statusId == INVENTORY_STATUS_ID_BAD ? 0m : (decimal?)null,
                    inventoryDetail = line.InventoryDetails.Where(x => x.Status?.Id == statusId).Select(y => new
                    {
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
        var lines = dto.Lines
            .Where(x => x.QuantityPlanned > x.QuantityReceived)
            .GroupBy(x => x.LineNumber)
            .Select(g => g.First());

        var obj = new
        {
            defaultValues = new {
                itemfulfillment = dto.ItemFulfillmentId
            },
            custbody_dbti_created_in_wms = true,
            custbody_dbti_receiving_category = isGood ? 1 : 2,
            custbody_dbti_prepared_by = dto.PreparedById,
            custbody_dbti_received_by = dto.PreparedById,
            item = new
            {
                items = dto.Lines.Select(line =>
                {
                    decimal lineQuantity = line.InventoryDetails.Sum(x => x.Status?.Id == statusId ? x.QuantityAlloted : 0);
                    bool isItemReceived = line.IsReceived && lineQuantity > 0 && line.InventoryDetails.Any(x => x.Status?.Id == statusId);

                    return new
                    {
                        itemreceive = isItemReceived,
                        orderLine = line.LineNumber,
                        quantity = isItemReceived ? lineQuantity : (decimal?)null,
                        custcol_dbti_actual_weight = isItemReceived ? line.WeightActual : (decimal?)null,
                        rate = isItemReceived && isGood ? (decimal?)null : 0,
                        inventoryDetail = isItemReceived ? new
                        {
                            inventoryAssignment = new
                            {
                                items = line.InventoryDetails.Where(x =>
                                    x.Status?.Id == statusId && x.QuantityAlloted > 0).Select(x =>
                                new
                                {
                                    inventoryStatus = statusId,
                                    binNumber = x.Bin?.Id,
                                    quantity = x.QuantityAlloted
                                })
                            }
                        } : null,
                        location = isItemReceived ? line.LocationId : (int?)null
                    };
                })
            },
            memo = "Created via WMS"
        };

        return JsonSerializer.Serialize(obj, JSON_OPTS);
    }

    private string CreatePOJson(ItemReceiptDTO dto, int statusId)
    {
        var lines = dto.Lines
            .Where(x => x.QuantityPlanned > x.QuantityReceived)
            .GroupBy(x => x.LineNumber)
            .Select(g => g.First());
        var isGood = statusId == INVENTORY_STATUS_ID_GOOD;
        var isMissing = statusId == INVENTORY_STATUS_ID_MISSING;

        var obj = new
        {
            defaultValues = new
            {
                itemfulfillment = dto.ItemFulfillmentId
            },
            custbody_dbti_created_in_wms = true,
            custbody_dbti_receiving_category = isMissing ? 5 : isGood ? 1 : 2,
            custbody_dbti_prepared_by = dto.PreparedById,
            custbody_dbti_received_by = dto.PreparedById,
            memo = dto.Remarks,
            item = new
            {
                items = lines.Select(line =>
                {
                    decimal lineQuantity = line.InventoryDetails.Sum(x => x.Status?.Id == statusId ? x.QuantityAlloted : 0);
                    bool isItemReceived = line.IsReceived && lineQuantity > 0 && line.InventoryDetails.Any(x => x.Status?.Id == statusId);

                    return new
                    {
                        itemreceive = isItemReceived,
                        orderLine = line.LineNumber,
                        quantity = isItemReceived ? lineQuantity : (decimal?)null,
                        custcol_dbti_actual_weight = isItemReceived ? line.WeightActual : (decimal?)null,
                        rate = isItemReceived && isGood ? (decimal?)null : 0,
                        inventoryDetail = isItemReceived ? new
                        {
                            inventoryAssignment = new
                            {
                                items = line.InventoryDetails.Where(x =>
                                    x.Status?.Id == statusId && x.QuantityAlloted > 0).Select(x =>
                                new
                                {
                                    inventoryStatus = statusId,
                                    binNumber = x.Bin?.Id,
                                    quantity = x.QuantityAlloted
                                })
                            }
                        } : null,
                        location = isItemReceived ? line.LocationId : (int?)null
                    };
                })
            },
            //memo = "Created via WMS"
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
