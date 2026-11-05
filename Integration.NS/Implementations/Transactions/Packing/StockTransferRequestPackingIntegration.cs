using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Exceptions;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Entities;
using Shared.Libraries.Utilities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class StockTransferRequestPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    ISqlQueryManager sqlQuery,
    SuiteQLQueryBuilderFactoryService builderFactory,
    ILogger<StockTransferRequestPackingIntegration> logger) : IStockTransferRequestPackingIntegration
{
    public async Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> GetPackingStockTransferRequestList(DataGridIntent intent, int subsidiaryId)
    {
        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StrPackingDataGridNSDTO.DateLastModified)));

        var (items, totalResults) = await QueryListWithOffsetClampAsync(intent, "Packing STR list", async skipOverride =>
        {
            var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(StrPackingDataGridNSDTO.Id)),
                ("t.tranid", nameof(StrPackingDataGridNSDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingDataGridNSDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingDataGridNSDTO.DateLastModified)),
                ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingDataGridNSDTO.SourceSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingDataGridNSDTO.DestinationSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingDataGridNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingDataGridNSDTO.TransferLocation)),
                ("s.name", nameof(StrPackingDataGridNSDTO.Status)),
                ("t.memo", nameof(StrPackingDataGridNSDTO.Remarks))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("transferorderstatus s", on: "s.id = t.status")
            .InnerJoin(SuiteQLFragments.PendingPackingLines(), on: SuiteQLFragments.PendingPackingLinesJoinPredicate)
            .WithFilters(
                Equal("tl.mainline", "T"),
                Any(
                    All(
                        Equal("t.recordtype", "transferorder"),
                        Equal("t.subsidiary", subsidiaryId)
                    ),
                    All(
                        Equal("t.recordtype", "intercompanytransferorder"),
                        Equal("t.subsidiary", subsidiaryId)
                    )
                )
            )
            .WithFilters(PackingStockTransferRequestFilters())
            .WithDatagridIntent(intent)
            .Build();

            if (skipOverride.HasValue) query.Offset = skipOverride.Value;

            var response = await query.ExecuteWithPaging<StrPackingDataGridNSDTO>(netsuiteService);

            return response;
        });

        return (items.Select(MapDataGridDto), totalResults);
    }

    /// <summary>
    /// Runs a paged list query with a single offset-clamp retry. NetSuite
    /// rejects any offset past the end of its (possibly empty) result set, and
    /// persisted Radzen pager positions routinely request such offsets once a
    /// result set shrinks or empties. Falls back to the last legal page using
    /// NetSuite's "between 0 and N" hint, else the first page. A second
    /// OffsetOutOfBounds means the data moved under us; surface it rather than
    /// looping. Mirrors the helper of the same name in ReceivingIntegration.
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

    public async Task<StockTransferRequestInfoPackingDTO?> GetPackingStockTransferRequest(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(StrPackingHeaderNSDTO.Id)),
                ("NVL(tte.id, 0)", nameof(StrPackingHeaderNSDTO.TripTicketExemptionId)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingHeaderNSDTO.Date)),
                ("t.tranid", nameof(StrPackingHeaderNSDTO.ReferenceNumber)),
                ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingHeaderNSDTO.FromSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingHeaderNSDTO.ToSubsidiary)),
                ("tl.location", nameof(StrPackingHeaderNSDTO.LocationId)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingHeaderNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingHeaderNSDTO.TransferLocation)), 
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StrPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .InnerJoin(SuiteQLFragments.PendingPackingLines(), on: SuiteQLFragments.PendingPackingLinesJoinPredicate)
            .LeftJoin("employee e", "t.custbody_dbti_prepared_by = e.id")
            .LeftJoin("customrecord_dbti_trip_ticket_exemption tte", "tte.custrecord_dbti_tte_from_location = tl.location AND tte.custrecord_dbti_tte_to_location = t.transferlocation AND tte.isinactive = 'F'")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.mainline", "T"))
            .WithFilters(PackingStockTransferRequestFilters())
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StrPackingHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items?.FirstOrDefault();

        return nsdto is null ? null : MapInfoDto(nsdto);
    }

    public async Task<bool?> GetTripTicketExemption(string tranid)
    {
        if (string.IsNullOrWhiteSpace(tranid)) return null;

        // No packing filters here on purpose. Exemption belongs to the location pair, so it must
        // still resolve when the order has advanced past the packing screen's filters. Reusing
        // GetPackingStockTransferRequest would return null in exactly that case and the caller
        // would post a Packed ship status for an exempt order.
        var query = builderFactory.Create()
            .Select(
                ("NVL(MIN(tte.id), 0)", nameof(TripTicketExemptionNSDTO.ExemptionId)))
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("customrecord_dbti_trip_ticket_exemption tte", SuiteQLFragments.TripTicketExemptionJoin)
            .WithFilters(
                Equal("t.tranid", tranid),
                Equal("tl.mainline", "T"),
                In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }))
            .GroupBy("t.id")
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TripTicketExemptionNSDTO>(query.Query, query.Limit, query.Offset);
        var row = response.items?.FirstOrDefault();

        return row is null ? null : row.ExemptionId != 0;
    }

    /// <summary>
    /// Loads packing lines for one transfer order by wrapping the
    /// NS_TO_x_Packing_Get_Items script as a derived table and projecting the
    /// grid columns.
    /// <para>
    /// Shape notes for the script (kept here, not in the .sql, because a
    /// <c>--</c> comment in a script is silently fatal — see
    /// <c>SuiteQLText.StripComments</c>):
    /// the ibq and ivb derived tables each collapse to one row per
    /// (item, location) and (item, subsidiary, location) via GROUP BY on a
    /// MIN(). Without those GROUP BYs they fan a single transaction line out
    /// into several rows when the item has multiple preferred bins at the
    /// location, or the preferred vendor has multiple bin-assignment records
    /// there. The mobile app posts every row back, and two body entries with
    /// the same orderLine are invalid for the !transform/itemFulfillment POST.
    /// </para>
    /// </summary>
    public async Task<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)> GetPackingStockTransferRequestLines(string id, DataGridIntent intent)
    {
        var mobileLineQuery = sqlQuery.ResolveSuiteQLScript(
            "NS_TO_x_Packing_Get_Items",
            new Dictionary<string, string>
            {
                ["tranid"] = id,
                ["vendorId"] = "0"
            });

        var query = builderFactory.Create()
            .Select(
                ("q.MaterialCode", nameof(StrPackingLineNSDTO.ItemCode)),
                ("q.MaterialName", nameof(StrPackingLineNSDTO.ItemDescription)),
                ("q.UoMName", nameof(StrPackingLineNSDTO.UoM)),
                ("q.LocationName", nameof(StrPackingLineNSDTO.Warehouse)),
                ("q.LocationItemQuantityAvailable", nameof(StrPackingLineNSDTO.QuantityAvailable)),
                ("(q.LineQuantity / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityPlanned)),
                ("(q.LineQuantityPacked / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityReceived)),
                ("(q.LineQuantityBackOrdered / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityBackOrdered)),
                ("q.IsClosed", nameof(StrPackingLineNSDTO.IsClosed)),
                ("(q.QuantityAlreadyShipped / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityAlreadyShipped))
            )
            .From($"({mobileLineQuery}) q")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<StrPackingLineNSDTO>(netsuiteService);

        return (response.items.Select(MapLineDto), response.totalResults);
    }

    /// <summary>
    /// Order-state predicates for the packing screens. The "still has quantity to pack"
    /// half of the filter is not here — it is the <see cref="SuiteQLFragments.PendingPackingLines"/>
    /// inner join, because it needs a derived table rather than a column comparison. Both must
    /// be applied together: these predicates alone let a fully packed order back onto the list
    /// until the Trip Ticket ships it (<c>ordpicked</c> tracks picking, and status stays in
    /// B/D/E while the fulfillment is merely Packed).
    /// </summary>
    private static AppFilterDescriptor[] PackingStockTransferRequestFilters()
    {
        return
        [
            In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
            In("t.custbody_dbti_transfer_category", new string[] { "1", "2" }),
            Equal("t.ordpicked", "F"),
            In("t.status", new string[] { "B", "D", "E" })
        ];
    }

    private static StockTransferRequestPackingDataGridDTO MapDataGridDto(StrPackingDataGridNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            ReferenceNumber = nsdto.ReferenceNumber,
            Date = nsdto.Date,
            SourceSubsidiary = nsdto.SourceSubsidiary,
            DestinationSubsidiary = nsdto.DestinationSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            Status = nsdto.Status,
            Remarks = nsdto.Remarks
        };
    }

    private static StockTransferRequestInfoPackingDTO MapInfoDto(StrPackingHeaderNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            Date = nsdto.Date,
            ReferenceNumber = nsdto.ReferenceNumber,
            FromSubsidiary = nsdto.FromSubsidiary,
            ToSubsidiary = nsdto.ToSubsidiary,
            LocationId = nsdto.LocationId,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            IsTripTicketExempt = nsdto.TripTicketExemptionId != 0,
            PreparedBy = nsdto.PreparedBy,
        };
    }

    private static StockTransferRequestLinePackingDTO MapLineDto(StrPackingLineNSDTO nsdto)
    {
        return new()
        {
            ItemCode = nsdto.ItemCode,
            ItemDescription = nsdto.ItemDescription,
            UoM = nsdto.UoM,
            Warehouse = nsdto.Warehouse,
            QuantityAvailable = nsdto.QuantityAvailable,
            QuantityPlanned = nsdto.QuantityPlanned,
            QuantityReceived = nsdto.QuantityReceived,
            QuantityBackOrdered = nsdto.QuantityBackOrdered,
            IsClosed = nsdto.IsClosed,
            QuantityAlreadyShipped = nsdto.QuantityAlreadyShipped,
        };
    }
}
