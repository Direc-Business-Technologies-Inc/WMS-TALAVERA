using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsReturn;

public record GetGoodsReturnRequestQry(int DocEntry) : IRequest<GoodsReturnRequestDTO?>;

public class GetGoodsReturnRequestQryHandler(
    IGoodsReturnIntegration goodsReturnIntegration) 
    : IRequestHandler<GetGoodsReturnRequestQry, GoodsReturnRequestDTO?>
{
    public async Task<GoodsReturnRequestDTO?> Handle(GetGoodsReturnRequestQry request, CancellationToken cancellationToken)
    {
        GRRHeaderSAPDTO? headerResponse = await goodsReturnIntegration.GetGRRHeaderAsync(request.DocEntry);

        if (headerResponse is null)
            return null;

        IEnumerable<GRRLineSAPDTO> linesResponse = await goodsReturnIntegration.GetGRRLinesAsync(request.DocEntry);

        GoodsReturnRequestDTO data = headerResponse.Adapt<GoodsReturnRequestDTO>();
        IEnumerable<GRRLineDTO> lines = linesResponse.Adapt<IEnumerable<GRRLineDTO>>();

        data.DocumentLines = [.. lines];

        return data;
    }
}