using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetTripTicketFulfillmentsQry(int Id) : IRequest<IEnumerable<TripTicketFulfillmentDTO>>;

public class GetTripTicketFulfillmentsQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetTripTicketFulfillmentsQry, IEnumerable<TripTicketFulfillmentDTO>>
{
    public Task<IEnumerable<TripTicketFulfillmentDTO>> Handle(GetTripTicketFulfillmentsQry request, CancellationToken cancellationToken)
    {
        return integration.GetTripTicketFulfillmentsAsync(request.Id);
    }
}
