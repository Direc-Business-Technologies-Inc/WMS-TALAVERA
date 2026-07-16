using Application.DataTransferObjects.Transactions.Packing;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Packing
{
    public record GetPackedItemFulfillmentsQry(DataGridIntent Intent) : IRequest<(IEnumerable<PackedItemFulfillmentDTO>, int)>;

    public class GetPackedItemFulfillmentsQryHandler(IReturnPackingIntegration integration)
        : IRequestHandler<GetPackedItemFulfillmentsQry, (IEnumerable<PackedItemFulfillmentDTO>, int)>
    {
        public Task<(IEnumerable<PackedItemFulfillmentDTO>, int)> Handle(GetPackedItemFulfillmentsQry request, CancellationToken cancellationToken)
        {
            return integration.GetPackedItemFulfillments(request.Intent);
        }
    }
}
