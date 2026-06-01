using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsReturn;

public record GetGoodsReturnRequestsQry(DataGridIntent Intent) : IRequest<(IEnumerable<GRRDataGridDTO> Data, int Count)>;

public class GetGoodsReturnRequestsQryHandler(
    IGoodsReturnIntegration goodsReturnIntegration)
    : IRequestHandler<GetGoodsReturnRequestsQry, (IEnumerable<GRRDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GRRDataGridDTO> Data, int Count)> Handle(GetGoodsReturnRequestsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsReturnRequestsSAPDTO> Data, int Count) = await goodsReturnIntegration.GetGRRsListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<GRRDataGridDTO>>(), Count);
    }
}
