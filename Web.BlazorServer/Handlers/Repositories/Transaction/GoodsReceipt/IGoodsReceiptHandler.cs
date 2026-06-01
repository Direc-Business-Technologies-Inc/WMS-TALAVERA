using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;
using Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReceipt;

public interface IGoodsReceiptHandler
{
    Task<(IEnumerable<GoodsReceiptDataGridVM> Data, int Count)> GetGoodsReceiptDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<GoodsReceiptDataGridVM> Data, int Count)> GetGoodsReceiptDataGridAsync(DataGridIntent intent, string status);
    Task<GoodsReceiptVM?> GetGoodsReceiptAsync(int docEntry);
    Task<GoodsReceiptVM?> GetDraftGoodsReceiptAsync(int docEntry);
    Task<bool> PostGoodsReceiptAsync(GoodsReceiptVM data);
}
