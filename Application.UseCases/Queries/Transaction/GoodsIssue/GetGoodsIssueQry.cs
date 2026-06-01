using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsIssue;

public record GetGoodsIssueQry(int DocEntry) : IRequest<GoodsIssueDTO?>;

public class GetGoodsIssueQryHandler(
    IGoodsIssueIntegration giIntegration
    ) : IRequestHandler<GetGoodsIssueQry, GoodsIssueDTO?>
{
    public async Task<GoodsIssueDTO?> Handle(GetGoodsIssueQry request, CancellationToken cancellationToken)
    {
        GoodsIssueHeaderSAPDTO? gi = await giIntegration.GetGoodsIssueHeader(request.DocEntry);
        if(gi is null)
            return null;
        IEnumerable<GoodsIssueLineSAPDTO> giLines = await giIntegration.GetGoodsIssueLines(request.DocEntry);
        GoodsIssueDTO dto = gi.Adapt<GoodsIssueDTO>();
        dto.DocumentLines = [.. giLines.Adapt<IEnumerable<GoodsIssueLineDTO>>()];
        return dto;
    }
}   
