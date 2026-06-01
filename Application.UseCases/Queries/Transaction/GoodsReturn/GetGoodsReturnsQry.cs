using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsReturn;

public record GetGoodsReturnsQry(DataGridIntent Intent) : IRequest<(IEnumerable<GoodsReturnDataGridDTO> Data, int Count)>;

public class GetGoodsReturnsQryHandler(
    IGoodsReturnIntegration goodsReturnIntegration) 
    : IRequestHandler<GetGoodsReturnsQry, (IEnumerable<GoodsReturnDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GoodsReturnDataGridDTO> Data, int Count)> Handle(GetGoodsReturnsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsReturnsSAPDTO> Data, int Count) = await goodsReturnIntegration.GetGoodsReturnsListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<GoodsReturnDataGridDTO>>(), Count);
    }
}
