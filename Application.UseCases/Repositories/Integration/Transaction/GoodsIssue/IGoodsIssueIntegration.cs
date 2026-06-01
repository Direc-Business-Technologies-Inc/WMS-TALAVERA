using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;

public interface IGoodsIssueIntegration
{
    Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetApprovedGoodsIssueDataGrid(DataGridIntent intent);
    Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetGoodsIssueDraftDataGrid(DataGridIntent intent, string status);
    Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueHeader(int docEntry);
    Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueLines(int docEntry);
    Task<bool> PostGoodsIssue(GoodsIssueDTO data);
    Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueDraftHeader(int docEntry);
    Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueDraftLines(int docEntry);
}
