using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using MediatR;

namespace Application.UseCases.Commands.Transaction.GoodsReceipt;

public record PostGoodsReceiptCmd(GoodsReceiptDTO Data) : ITransactionalRequest<bool>;

public class PostGoodsReceiptCmdHandler(
    IGoodsReceiptIntegration grIntegration) 
    : IRequestHandler<PostGoodsReceiptCmd, bool>
{
    public async Task<bool> Handle(PostGoodsReceiptCmd request, CancellationToken cancellationToken)
    {
        bool result = await grIntegration.PostGoodsReceipt(request.Data);
        return result;
    }
}
