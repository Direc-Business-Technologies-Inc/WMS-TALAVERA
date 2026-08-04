using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions;

public class TripTicketIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory)
    : ITripTicketIntegration
{
    public async Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> GetTripTicketsAsync(DataGridIntent intent, int subsidiaryId)
    {
        if (intent.Sorts.Count == 0) intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(TripTicketDataGridDTO.TripDate)));
        var query = CreateTripTicketListQuery()
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<TripTicketDataGridDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }

    public async Task<TripTicketDataGridDTO?> GetTripTicketAsync(int id)
    {
        var query = CreateTripTicketListQuery()
            .WithFilters(Equal("NetsuiteTripTicketInternalId", id))
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TripTicketDataGridDTO>(
            query.Query,
            query.Limit,
            query.Offset);

        return response.items.FirstOrDefault();
    }

    public async Task<IEnumerable<TripTicketFulfillmentDTO>> GetTripTicketFulfillmentsAsync(int id)
    {
        var query = builderFactory.Create()
            .Select(
                ("tt.id", nameof(TripTicketFulfillmentDTO.NetsuiteTripTicketInternalId)),
                ("t.id", nameof(TripTicketFulfillmentDTO.NetsuiteOrderInternalId)),
                ("t.tranid", nameof(TripTicketFulfillmentDTO.OrderNumber)),
                ("BUILTIN.DF(t.transferlocation)", nameof(TripTicketFulfillmentDTO.DestinationLocation)))
            .From("customrecord_dbti_trip_ticket tt")
            .Join("customrecord_dbti_trip_ticket_if ttif", on: "tt.id = ttif.custrecord_dbti_ttf_trip_ticket_num")
            .Join("transaction t", on: "ttif.custrecord_dbti_ttf_item_fulfillment_num = t.id")
            .WithFilters(
                Equal("t.type", "ItemShip"),
                Equal("tt.id", id))
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<TripTicketFulfillmentDTO>(
            query.Query,
            query.Limit,
            query.Offset);

        return response.items;
    }

    private SuiteQLQueryBuilder CreateTripTicketListQuery()
    {
        var transactionBuilder = builderFactory.Create()
            .Select(
                ("COUNT(t.id)", "MatchedSubsidiaries"),
                ("ttf.custrecord_dbti_ttf_trip_ticket_num", "TicketNumber")
            )
            .From("transaction t")
            .Join("customrecord_dbti_trip_ticket_if ttf", on: "ttf.custrecord_dbti_ttf_item_fulfillment_num = t.id")
            .WithSubsidiaries(httpContextAccessor, "t")
            .GroupBy("TicketNumber")
            .Build();

        return builderFactory.Create()
            .Select(
                ("tt.id", nameof(TripTicketDataGridDTO.NetsuiteTripTicketInternalId)),
                ("tt.name", nameof(TripTicketDataGridDTO.Name)),
                ("tp.name", nameof(TripTicketDataGridDTO.TruckPlateNumber)),
                ("tp.id", nameof(TripTicketDataGridDTO.TruckPlateNumberId)),
                ("BUILTIN.DF(tt.custrecord_dbti_destination)", nameof(TripTicketDataGridDTO.Destination)),
                ("CONCAT(e.firstname, CONCAT(' ', e.lastname))", nameof(TripTicketDataGridDTO.Driver)),
                ("CONCAT(eh.firstname, CONCAT(' ', eh.lastname))", nameof(TripTicketDataGridDTO.HelperName)),
                ("eh.id", nameof(TripTicketDataGridDTO.HelperId)),
                ("BUILTIN.DF(tt.custrecord_dbti_trt_origin_location)", nameof(TripTicketDataGridDTO.Location)),
                ("TO_CHAR(tt.custrecord_dbti_trt_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(TripTicketDataGridDTO.TripDate)),
                ("tt.custrecord_dbti_trt_truck_seal", nameof(TripTicketDataGridDTO.TruckSeal))
            )
            .From("customrecord_dbti_trip_ticket tt")
            .LeftJoin(($"({transactionBuilder.Query}) ms" ), "ms.TicketNumber = tt.id")
            .LeftJoin("employee e", "e.id = tt.custrecord_dbti_trt_assigned_driver")
            .LeftJoin("MAP_customrecord_dbti_trip_ticket_custrecord_dbti_trt_helper m1", "tt.id = m1.mapone")
            .LeftJoin("employee eh", "eh.id = m1.maptwo")
            .LeftJoin("CUSTOMRECORD_DBTI_TRUCK_PLATE_NO tp", "tp.id = tt.custrecord_dbti_trp_truck_plate_no")
            .WithFilter(
                GreaterThan("ms.MatchedSubsidiaries", 0));
    }
}
