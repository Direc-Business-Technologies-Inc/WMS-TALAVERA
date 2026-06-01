using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SalesReturn;

public record GetReturnTypesQry() : IRequest<IEnumerable<ReturnTypeDTO>>;

public class GetReturnTypesQryHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<GetReturnTypesQry, IEnumerable<ReturnTypeDTO>>
{
    public async Task<IEnumerable<ReturnTypeDTO>> Handle(GetReturnTypesQry request, CancellationToken cancellationToken)
    {
        IEnumerable<ReturnTypeSAPDTO> returnTypes = await salesReturnIntegration.GetReturnTypesAsync();
        return returnTypes.Adapt<IEnumerable<ReturnTypeDTO>>();
    }
}
