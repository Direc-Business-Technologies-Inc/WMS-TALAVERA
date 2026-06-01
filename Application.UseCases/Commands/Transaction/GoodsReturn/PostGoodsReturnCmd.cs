using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Domain.Entities.Enums.Transaction.GoodsReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Commands.Transaction.GoodsReturn;

public record PostGoodsReturnCmd(GoodsReturnRequestDTO Data, GoodsReturnPostingSource Source) : IRequest<bool>;

public class PostGoodsReturnCmdHandler(
    IGoodsReturnIntegration goodsReturnIntegration)
    : IRequestHandler<PostGoodsReturnCmd, bool>
{
    public async Task<bool> Handle(PostGoodsReturnCmd request, CancellationToken cancellationToken)
    {
        GoodsReturnDTO payload = request.Data.Adapt<GoodsReturnDTO>();
        bool result = false;
        if (request.Source.Equals(GoodsReturnPostingSource.Standalone))
            result = await goodsReturnIntegration.PostGoodsReturnAsync(payload);
        else if (request.Source.Equals(GoodsReturnPostingSource.GRPO))
            result = await goodsReturnIntegration.PostGoodsReturnFromGRPOAsync(payload);
        else if (request.Source.Equals(GoodsReturnPostingSource.GRR))
            result = await goodsReturnIntegration.PostGoodsReturnFromGRRAsync(payload);

        return result;
    }
}
