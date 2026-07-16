using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetItemFulfillmentsForTransferOrderQry(int toId, DataGridIntent Intent) : IRequest<(IEnumerable<ItemFulfillmentDTO>, int)>;

public class GetItemFulfillmentsForTransferOrderQryHandler(
    IReceivingIntegration integration
    ) : IRequestHandler<GetItemFulfillmentsForTransferOrderQry, (IEnumerable<ItemFulfillmentDTO>, int)>
{
    public Task<(IEnumerable<ItemFulfillmentDTO>, int)> Handle(GetItemFulfillmentsForTransferOrderQry request, CancellationToken cancellationToken)
    {
        return integration.GetSTRItemFulfillments(request.toId, request.Intent);
    }
}
