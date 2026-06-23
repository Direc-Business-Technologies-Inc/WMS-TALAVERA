using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryAdjustment;

public record GetInventoryAdjustmentReasonsQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)>;

public class GetInventoryAdjustmentReasonsQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetInventoryAdjustmentReasonsQry, (IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)> Handle(GetInventoryAdjustmentReasonsQry request, CancellationToken cancellationToken)
    {
        return await integration.GetInventoryAdjustmentReasonsAsync(request.Intent);
    }
}

