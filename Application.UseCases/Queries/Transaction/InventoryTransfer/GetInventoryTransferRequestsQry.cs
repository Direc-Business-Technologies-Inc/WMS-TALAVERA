using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Mapster;
using MediatR;
using Shared.Entities;
using DataGridDTO = Application.DataTransferObjects.Transactions.InventoryTransfer.InventoryTransferDataGridDTO;
using DataGridSAPDTO = Integration.SAP.Entities.Transactional.InventoryTransfer.InventoryTransferDataGridSAPDTO;

namespace Application.UseCases.Queries.Transaction.InventoryTransfer;

public record GetInventoryTransferRequestsQry(DataGridIntent Intent) : IRequest<(IEnumerable<DataGridDTO> Data, int Count)>;

public class GetInventoryTransferRequestsQryHandler(
        IInventoryTransferIntegration inventoryTransferIntegration)
        : IRequestHandler<GetInventoryTransferRequestsQry, (IEnumerable<DataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<DataGridDTO> Data, int Count)> Handle(
        GetInventoryTransferRequestsQry request,
        CancellationToken cancellationToken
    )
    {
        (IEnumerable<DataGridSAPDTO>? Data, int Count) = await inventoryTransferIntegration.GetInventoryTransferRequestListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<DataGridDTO>>(), Count);
    }
}

