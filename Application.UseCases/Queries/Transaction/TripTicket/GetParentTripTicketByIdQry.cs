using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetParentTripTicketByIdQry(int Id) : IRequest<TripTicketDataGridDTO?>;

public class GetParentTripTicketByIdQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetParentTripTicketByIdQry, TripTicketDataGridDTO?>
{
    public Task<TripTicketDataGridDTO?> Handle(GetParentTripTicketByIdQry request, CancellationToken cancellationToken)
    {
        return integration.GetParentTripTicketAsync(request.Id);
    }
}
