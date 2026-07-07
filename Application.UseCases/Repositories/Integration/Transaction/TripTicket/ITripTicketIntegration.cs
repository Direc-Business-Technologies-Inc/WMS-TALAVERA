using Application.DataTransferObjects.Transactions.TripTicket;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.TripTicket;

public interface ITripTicketIntegration
{
    Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> GetTripTicketsAsync(DataGridIntent intent, int subsidiaryId);
    Task<TripTicketDataGridDTO?> GetTripTicketAsync(int id);
    Task<IEnumerable<TripTicketFulfillmentDTO>> GetTripTicketFulfillmentsAsync(int id);
}
