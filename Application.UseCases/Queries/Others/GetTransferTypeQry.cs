using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.DataTransferObjects.Transactions.InventoryTransfer.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetTransferTypeQry() : IRequest<IEnumerable<TransferTypeDTO>>;

public class GetTransferTypeQryHandler(
    ITransferTypeIntegration transferTypeIntegration
    ): IRequestHandler<GetTransferTypeQry, IEnumerable<TransferTypeDTO>>
{
    public async Task<IEnumerable<TransferTypeDTO>> Handle(GetTransferTypeQry request, CancellationToken cancellationToken)
    {
        IEnumerable<TransferTypeSAPDTO> response = await transferTypeIntegration.GetTransferTypesAsync();

        return response.Adapt<IEnumerable<TransferTypeDTO>>();
    }
}

