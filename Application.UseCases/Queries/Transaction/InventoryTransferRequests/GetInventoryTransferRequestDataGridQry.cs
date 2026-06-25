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

public record GetInventoryTransferRequestDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryTransferRequestDataGridDTO>, int)>;

public class GetInventoryTransferRequestDataGridQryHandler(
    IInventoryTransferRequestIntegration integration)
    : IRequestHandler<GetInventoryTransferRequestDataGridQry, (IEnumerable<InventoryTransferRequestDataGridDTO>, int)>
{
    public async Task<(IEnumerable<InventoryTransferRequestDataGridDTO>, int)> Handle(GetInventoryTransferRequestDataGridQry request, CancellationToken cancellationToken)
    {
        return await integration.GetInventoryTransferRequestsDataGridAsync(request.Intent);
    }
}
