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

public record GetItemFulfillmentsForTransferOrderQry(int toId, DataGridIntent Intent) : IRequest<ItemFulfillmentPickerResult>;

public class GetItemFulfillmentsForTransferOrderQryHandler(
    IReceivingIntegration integration
) : IRequestHandler<GetItemFulfillmentsForTransferOrderQry, ItemFulfillmentPickerResult>
{
    public Task<ItemFulfillmentPickerResult> Handle(GetItemFulfillmentsForTransferOrderQry request, CancellationToken cancellationToken)
    {
        return integration.GetSTRItemFulfillments(request.toId, request.Intent);
    }
}
