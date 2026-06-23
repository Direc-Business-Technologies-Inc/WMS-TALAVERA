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

public record GetInventoryAdjustmentsQry(DataGridIntent intent) : IRequest<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>;

public class GetInventoryAdjustmentsQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetInventoryAdjustmentsQry, (IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> Handle(GetInventoryAdjustmentsQry request, CancellationToken cancellationToken)
    {
        return await integration.GetInventoryAdjustmentsAsync(request.intent);
    }
}
