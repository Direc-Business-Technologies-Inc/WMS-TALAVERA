using Application.DataTransferObjects.Transactions.ItemFulfillment;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.TripTicket;

public record GetItemFulfillmentsPackedQry(DataGridIntent intent) : IRequest<(IEnumerable<ItemFulfillmentDTO>, int)>;

public class GetItemFulfillmentsPackedQryHandler(ITripTicketIntegration integration)
    : IRequestHandler<GetItemFulfillmentsPackedQry, (IEnumerable<ItemFulfillmentDTO>, int)>
{
    public Task<(IEnumerable<ItemFulfillmentDTO>, int)> Handle(GetItemFulfillmentsPackedQry request, CancellationToken cancellationToken)
    {
        return integration.GetItemfulfillments(request.intent);
    }
}