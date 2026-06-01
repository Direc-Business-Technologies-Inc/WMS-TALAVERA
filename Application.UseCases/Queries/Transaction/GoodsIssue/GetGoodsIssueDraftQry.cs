using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsIssue;

public record GetGoodsIssueDraftQry(int DocEntry) : IRequest<GoodsIssueDTO?>;

public class GoodsIssueDraftQryHandler(
    IGoodsIssueIntegration giIntegration
    ) : IRequestHandler<GetGoodsIssueDraftQry, GoodsIssueDTO?>
{
    public async Task<GoodsIssueDTO?> Handle(GetGoodsIssueDraftQry request, CancellationToken cancellationToken)
    {
        GoodsIssueHeaderSAPDTO? giDraft = await giIntegration.GetGoodsIssueDraftHeader(request.DocEntry);
        if(giDraft is null)
            return null;
        IEnumerable<GoodsIssueLineSAPDTO> giDraftLines = await giIntegration.GetGoodsIssueDraftLines(request.DocEntry);
        GoodsIssueDTO dto = giDraft.Adapt<GoodsIssueDTO>();
        dto.DocumentLines = [.. giDraftLines.Adapt<IEnumerable<GoodsIssueLineDTO>>()];
        return dto;
    }
}