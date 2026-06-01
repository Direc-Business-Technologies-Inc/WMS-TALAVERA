using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using MediatR;

namespace Application.UseCases.Commands.Transaction.GoodsIssue;

public record PostGoodsIssueCmd(GoodsIssueDTO Data) : ITransactionalRequest<bool>;

public class PostGoodsIssueCmdHandler(
    IGoodsIssueIntegration goodsIssueIntegration) 
    : IRequestHandler<PostGoodsIssueCmd, bool>
{
    public async Task<bool> Handle(PostGoodsIssueCmd request, CancellationToken cancellationToken)
    {
        bool result = await goodsIssueIntegration.PostGoodsIssue(request.Data);

        return result;
    }
}   
