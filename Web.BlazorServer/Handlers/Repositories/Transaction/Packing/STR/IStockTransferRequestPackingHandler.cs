using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;

public interface IStockTransferRequestPackingHandler
{
    Task<(IEnumerable<StockTransferRequestPackingDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent);
    Task<StockTransferRequestInfoPackingVM?> GetPackingStockTransferRequest(string reference, bool includeLines = true);
    Task<(IEnumerable<TransferOrderStatusPackingVM> data, int count)> GetTransferOrderStatuses(DataGridIntent intent);
}
