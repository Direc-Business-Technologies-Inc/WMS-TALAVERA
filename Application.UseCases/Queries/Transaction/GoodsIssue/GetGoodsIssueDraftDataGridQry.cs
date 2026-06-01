using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsIssue;

public record GetGoodsIssueDraftDataGridQry(DataGridIntent Intent, string Status) : IRequest<(IEnumerable<GoodsIssueDataGridDTO> Data, int Count)>;

public class GetGoodsIssueDraftDataGridQryHandler(
    IGoodsIssueIntegration giIntegration)
    : IRequestHandler<GetGoodsIssueDraftDataGridQry, (IEnumerable<GoodsIssueDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GoodsIssueDataGridDTO> Data, int Count)> Handle(GetGoodsIssueDraftDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count) = await giIntegration.GetGoodsIssueDraftDataGrid(request.Intent, request.Status);

        return (Data.Adapt<IEnumerable<GoodsIssueDataGridDTO>>(), Count);
    }
}
