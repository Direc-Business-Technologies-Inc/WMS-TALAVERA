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

public record GetItemFulfillmentLinesQry(int ifId, DataGridIntent Intent) : IRequest<IEnumerable<ItemFulfillmentLineDTO>>;

public class GetItemFulfillmentLinesQryHandler(
        IReceivingIntegration integration
    ) : IRequestHandler<GetItemFulfillmentLinesQry, IEnumerable<ItemFulfillmentLineDTO>>
{
    public Task<IEnumerable<ItemFulfillmentLineDTO>> Handle(GetItemFulfillmentLinesQry request, CancellationToken cancellationToken)
    {
        return integration.GetItemFulfillmentLines(request.ifId, request.Intent);
    }
}