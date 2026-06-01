using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryTransfer
{
    public record GetInventoryTransferRequestQry(int DocEntry) : IRequest<InventoryTransferRequestDTO>;
    public class GetInventoryTransferRequestQryHandler(
        IInventoryTransferIntegration inventoryTransferIntegration
    ): IRequestHandler<GetInventoryTransferRequestQry, InventoryTransferRequestDTO?>
    {
        public async Task<InventoryTransferRequestDTO?> Handle(GetInventoryTransferRequestQry request, CancellationToken cancellationToken)
        {
            InventoryTransferRequestHeaderSAPDTO? headerResponse = await inventoryTransferIntegration.GetInventoryTransferRequestHeaderAsync(request.DocEntry);

            if (headerResponse is null)
                return null;

            IEnumerable<InventoryTransferRequestLineSAPDTO> linesResponse = await inventoryTransferIntegration.GetInventoryTransferRequestLinesAsync(request.DocEntry);

            InventoryTransferRequestDTO inventoryTransferRequestDTO = headerResponse.Adapt<InventoryTransferRequestDTO>();
            IEnumerable<InventoryTransferRequestLineDTO> inventoryTransferRequestLinesDTO = linesResponse.Adapt<IEnumerable<InventoryTransferRequestLineDTO>>();

            inventoryTransferRequestDTO.Lines = [.. inventoryTransferRequestLinesDTO];

            return inventoryTransferRequestDTO;
        }
    }
}
