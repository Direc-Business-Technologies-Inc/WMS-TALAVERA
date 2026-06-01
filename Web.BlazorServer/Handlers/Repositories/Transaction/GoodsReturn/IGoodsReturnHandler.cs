using Domain.Entities.Enums.Transaction.GoodsReturn;
using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReturn;

public interface IGoodsReturnHandler
{
    Task<(IEnumerable<GRRDataGridVM> Data, int Count)> GetGoodsReturnRequestDataGridAsync(DataGridIntent intent);
    Task<GoodsReturnRequestVM?> GetGoodsReturnRequestAsync(int docEntry);
    Task<(IEnumerable<GoodsReturnDataGridVM> Data, int Count)> GetGoodsReturnDataGridAsync(DataGridIntent intent);
    Task<GoodsReturnVM?> GetGoodsReturnAsync(int docEntry);
    Task<bool> PostGoodsReturnAsync(GoodsReturnRequestVM data, GoodsReturnPostingSource source);
    Task<bool> PostGoodsReturnAsync(GoodsReturnVM data, GoodsReturnPostingSource source);
    Task<IEnumerable<ReturnTypeVM>> GetReturnTypesAsync();
}
