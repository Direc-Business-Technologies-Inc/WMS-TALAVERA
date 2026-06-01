using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseTypesQry() : IRequest<IEnumerable<PurchaseTypeDTO>>;

public class GetPurchaseTypesQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetPurchaseTypesQry, IEnumerable<PurchaseTypeDTO>>
{
    public async Task<IEnumerable<PurchaseTypeDTO>> Handle(GetPurchaseTypesQry request, CancellationToken cancellationToken)
    {
        IEnumerable<PurchaseTypeSAPDTO> response = await receivingIntegration.GetPurchaseTypesAsync();

        return response.Adapt<IEnumerable<PurchaseTypeDTO>>();
    }
}