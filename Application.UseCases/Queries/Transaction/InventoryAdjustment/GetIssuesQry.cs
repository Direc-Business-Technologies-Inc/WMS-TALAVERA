using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.InventoryAdjustment;

public record GetIssuesQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>;

public class GetIssuesQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetIssuesQry, (IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> Handle(GetIssuesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetIssuesAdjustmentsAsync(request.Intent);
    }
}   