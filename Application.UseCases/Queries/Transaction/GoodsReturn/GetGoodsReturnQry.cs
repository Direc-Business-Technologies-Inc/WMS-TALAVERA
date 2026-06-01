using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsReturn;

public record GetGoodsReturnQry(int DocEntry) : IRequest<GoodsReturnDTO?>;

public class GetGoodsReturnQryHandler(
    IGoodsReturnIntegration goodsReturnIntegration) 
    : IRequestHandler<GetGoodsReturnQry, GoodsReturnDTO?>
{
    public async Task<GoodsReturnDTO?> Handle(GetGoodsReturnQry request, CancellationToken cancellationToken)
    {
        GoodsReturnHeaderSAPDTO? headerResponse = await goodsReturnIntegration.GetGoodsReturnHeaderAsync(request.DocEntry);

        if(headerResponse is null)
            return null;

        IEnumerable<GoodsReturnLineSAPDTO> linesResponse = await goodsReturnIntegration.GetGoodsReturnLinesAsync(request.DocEntry);

        GoodsReturnDTO data = headerResponse.Adapt<GoodsReturnDTO>();
        IEnumerable<GoodsReturnLineDTO> lineData = linesResponse.Adapt<IEnumerable<GoodsReturnLineDTO>>();

        data.DocumentLines = [.. lineData];

        return data;

    }
}