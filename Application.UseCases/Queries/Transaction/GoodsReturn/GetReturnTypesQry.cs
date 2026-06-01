using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsReturn;

public record GetReturnTypesQry() : IRequest<IEnumerable<ReturnTypeDTO>>;

public class GetReturnTypesQryHandler(
    IGoodsReturnIntegration goodsReturnIntegration) 
    : IRequestHandler<GetReturnTypesQry, IEnumerable<ReturnTypeDTO>>
{
    public async Task<IEnumerable<ReturnTypeDTO>> Handle(GetReturnTypesQry request, CancellationToken cancellationToken)
    {
        IEnumerable<ReturnTypeSAPDTO> response = await goodsReturnIntegration.GetReturnTypesAsync();

        return response.Adapt<IEnumerable<ReturnTypeDTO>>();
    }
}
