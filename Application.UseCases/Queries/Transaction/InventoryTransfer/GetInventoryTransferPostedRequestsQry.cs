using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.InventoryTransfer;
    public record GetInventoryTransferPostedRequestsQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>;

    public class GetPostedInventoryTransferRequestsQryHandler(
        IInventoryTransferIntegration inventoryTransferIntegration )
        : IRequestHandler<GetInventoryTransferPostedRequestsQry, (IEnumerable<InventoryTransferDataGridDTO> Data, int Count)>
    {
        public async Task<(IEnumerable<InventoryTransferDataGridDTO> Data, int Count)> Handle(GetInventoryTransferPostedRequestsQry request, CancellationToken cancellationToken)
        {
            (IEnumerable<InventoryTransferDataGridSAPDTO> Data, int Count) = await inventoryTransferIntegration.GetPostedInventoryTransferRequestListAsync(request.Intent);

            return (Data.Adapt<IEnumerable<InventoryTransferDataGridDTO>>(), Count);
        }
    }

