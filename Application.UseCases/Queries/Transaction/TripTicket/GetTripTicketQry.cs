using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetTripTicketQry(int Id) : IRequest<TripTicketDataGridDTO?>;

public class GetTripTicketQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetTripTicketQry, TripTicketDataGridDTO?>
{
    public Task<TripTicketDataGridDTO?> Handle(GetTripTicketQry request, CancellationToken cancellationToken)
    {
        return integration.GetTripTicketAsync(request.Id);
    }
}
