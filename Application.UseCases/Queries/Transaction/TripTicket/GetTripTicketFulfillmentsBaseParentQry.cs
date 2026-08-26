using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetTripTicketFulfillmentsBaseParentQry(int Id) : IRequest<IEnumerable<TripTicketFulfillmentDTO>>;

public class GetTripTicketFulfillmentsBaseParentQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetTripTicketFulfillmentsBaseParentQry, IEnumerable<TripTicketFulfillmentDTO>>
{
    public Task<IEnumerable<TripTicketFulfillmentDTO>> Handle(GetTripTicketFulfillmentsBaseParentQry request, CancellationToken cancellationToken)
    {
        return integration.GetTripTicketFulfillmentsBaseParentAsync(request.Id);
    }
}