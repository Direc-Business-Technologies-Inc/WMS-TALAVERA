using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetParentTripTicketsQry(DataGridIntent intent) : IRequest<(IEnumerable<TripTicketDataGridDTO>, int)>;

public class GetParentTripTicketsQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetParentTripTicketsQry, (IEnumerable<TripTicketDataGridDTO>, int)>
{
    public Task<(IEnumerable<TripTicketDataGridDTO> , int)> Handle(GetParentTripTicketsQry request, CancellationToken cancellationToken)
    {
        return integration.GetParentTripTicketsAsync(request.intent);
    }
}
