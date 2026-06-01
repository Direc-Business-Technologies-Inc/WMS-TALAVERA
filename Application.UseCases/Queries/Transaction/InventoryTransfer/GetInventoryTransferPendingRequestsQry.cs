using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Mapster;
using MediatR;
using Shared.Entities;
namespace Application.UseCases.Queries.Transaction.InventoryTransfer;

public record GetInventoryTransferPendingRequestsQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>;

public class GetPendingInventoryTransferRequestsQryHandler (
        IInventoryTransferIntegration inventoryTransferIntegration)
        : IRequestHandler<GetInventoryTransferPendingRequestsQry, (IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)> Handle(
        GetInventoryTransferPendingRequestsQry request,
        CancellationToken cancellationToken
    )
    {
        (var Data, int Count) = await inventoryTransferIntegration.GetPendingInventoryTransferRequestListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<InventoryTransferDataGridDTO>>(), Count);
    }
}


