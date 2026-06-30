using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions;

public class TripTicketIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory)
    : ITripTicketIntegration
{
    public async Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> GetTripTicketsAsync(DataGridIntent intent)
    {
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

    private SuiteQLQueryBuilder CreateTripTicketListQuery()
    {
        return builderFactory.Create()
            .Select(
                ("tt.id", nameof(TripTicketDataGridDTO.NetsuiteTripTicketInternalId)),
                ("tt.name", nameof(TripTicketDataGridDTO.Name)),
                ("BUILTIN.DF(tt.custrecord_dbti_destination)", nameof(TripTicketDataGridDTO.Destination)),
                ("BUILTIN.DF(tt.custrecord_dbti_trt_assigned_driver)", nameof(TripTicketDataGridDTO.Driver)),
                ("BUILTIN.DF(tt.custrecord_dbti_trt_origin_location)", nameof(TripTicketDataGridDTO.Location)),
                ("TO_CHAR(tt.custrecord_dbti_trt_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(TripTicketDataGridDTO.TripDate)))
            .From("customrecord_dbti_trip_ticket tt");
    }
}
