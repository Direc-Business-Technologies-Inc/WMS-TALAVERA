using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetTripTicketDataGridQry(DataGridIntent Intent)
    : IRequest<(IEnumerable<TripTicketDataGridDTO> Data, int Count)>;

public class GetTripTicketDataGridQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetTripTicketDataGridQry, (IEnumerable<TripTicketDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> Handle(
        GetTripTicketDataGridQry request,
        CancellationToken cancellationToken)
    {
        return integration.GetTripTicketsAsync(request.Intent);
    }
}
