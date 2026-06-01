using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.GoodsIssue;

public interface IGoodsIssueHandler
{
    Task<(IEnumerable<GoodsIssueDataGridVM> Data, int Count)> GetGoodsIssueDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<GoodsIssueDataGridVM> Data, int Count)> GetGoodsIssueDataGridAsync(DataGridIntent intent, string status);
    Task<GoodsIssueVM> GetGoodsIssueAsync(int docEntry);
    Task<GoodsIssueVM> GetDraftGoodsIssueAsync(int docEntry);
    Task<bool> PostGoodsIssueAsync(GoodsIssueVM data);
}
