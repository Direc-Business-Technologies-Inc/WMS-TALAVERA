using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Mapster;
using MediatR;
using Shared.Entities;
namespace Application.UseCases.Queries.Transaction.InventoryTransfer;

public record GetInventoryTransferRejectedRequestsQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>;

public class GetRejectedInventoryTransferRequestsQryHandler (
        IInventoryTransferIntegration inventoryTransferIntegration)
        : IRequestHandler<GetInventoryTransferRejectedRequestsQry, (IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)> Handle(
        GetInventoryTransferRejectedRequestsQry request,
        CancellationToken cancellationToken
    )
    {
        (var Data, int Count) = await inventoryTransferIntegration.GetRejectedInventoryTransferRequestListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<InventoryTransferDataGridDTO>>(), Count);
    }
}


