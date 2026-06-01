using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Mapster;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryTransfer;

public record GetInventoryTransferPendingRequestQry(int DocEntry) : IRequest<InventoryTransferDTO?>;
public class GetPendingInventoryTransferRequestQryHandler(
        IInventoryTransferIntegration inventoryTransferIntegration
    ) : IRequestHandler<GetInventoryTransferPendingRequestQry, InventoryTransferDTO?>
{
    public async Task<InventoryTransferDTO?> Handle(GetInventoryTransferPendingRequestQry request, CancellationToken cancellationToken)
    {
        InventoryTransferHeaderSAPDTO? headerResponse = await inventoryTransferIntegration.GetInventoryTransferRequestDraftHeaderAsync(request.DocEntry, "W");
        if (headerResponse is null)
            return null;
        IEnumerable<InventoryTransferLineSAPDTO> linesResponse = await inventoryTransferIntegration.GetInventoryTransferRequestDraftLinesAsync(request.DocEntry);

        InventoryTransferDTO inventoryTransferRequestDTO = headerResponse.Adapt<InventoryTransferDTO>();
        IEnumerable<InventoryTransferLineDTO> inventoryTransferRequestLinesDTO = linesResponse.Adapt<IEnumerable<InventoryTransferLineDTO>>();

        inventoryTransferRequestDTO.Lines = [.. inventoryTransferRequestLinesDTO];

        return inventoryTransferRequestDTO;
    }
}
