using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.InventoryAdjustment;

public record GetReceiptsQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>;
public class GetReceiptsQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetReceiptsQry, (IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> Handle(GetReceiptsQry request, CancellationToken cancellationToken)
    {
        return await integration.GetReceiptsAdjustmentsAsync(request.Intent);
    }
}   
