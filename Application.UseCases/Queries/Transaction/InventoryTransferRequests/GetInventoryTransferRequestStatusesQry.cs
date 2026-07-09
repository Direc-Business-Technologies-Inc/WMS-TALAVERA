using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryTransferRequests;

public record GetInventoryTransferRequestStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryTransferRequestStatusDTO>, int)>;

public class GetInventoryTransferRequestStatusesQryHandler(IInventoryTransferRequestIntegration integration)
    : IRequestHandler<GetInventoryTransferRequestStatusesQry, (IEnumerable<InventoryTransferRequestStatusDTO>, int)>
{
    public Task<(IEnumerable<InventoryTransferRequestStatusDTO>, int)> Handle(GetInventoryTransferRequestStatusesQry request, CancellationToken cancellationToken)
    {
        return integration.GetStatusTypesAsync(request.Intent);
    }
}