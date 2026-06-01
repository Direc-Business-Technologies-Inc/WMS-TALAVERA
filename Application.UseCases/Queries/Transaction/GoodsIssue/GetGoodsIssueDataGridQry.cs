using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsIssue;

public record GetGoodsIssueDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<GoodsIssueDataGridDTO> Data, int Count)>;

public class GetGoodsIssueDataGridQryHandler(
    IGoodsIssueIntegration giIntegration) 
    : IRequestHandler<GetGoodsIssueDataGridQry, (IEnumerable<GoodsIssueDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GoodsIssueDataGridDTO> Data, int Count)> Handle(GetGoodsIssueDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count) = await giIntegration.GetApprovedGoodsIssueDataGrid(request.Intent);

        return (Data.Adapt<IEnumerable<GoodsIssueDataGridDTO>>(), Count);
    }
}
