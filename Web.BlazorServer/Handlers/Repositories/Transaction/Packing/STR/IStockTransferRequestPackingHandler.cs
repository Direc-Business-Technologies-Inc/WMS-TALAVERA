using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;

public interface IStockTransferRequestPackingHandler
{
    Task<(IEnumerable<StockTransferRequestPackingDataGridVM> Data, int Count)> GetStockTransferRequestsList(DataGridIntent intent, int subsidiaryId);
    Task<StockTransferRequestInfoPackingVM?> GetPackingStockTransferRequest(string reference);
    Task<(IEnumerable<StockTransferRequestLinePackingVM> Data, int Count)> GetPackingStockTransferRequestLines(string reference, DataGridIntent intent);
}
