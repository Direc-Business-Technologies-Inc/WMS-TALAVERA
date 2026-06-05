using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;

public interface IStockTransferRequestHandler
{
    Task<StockTransferRequestInfoVM?> GetStockTransferRequest(string reference, bool includeLines = false);
    Task<(IEnumerable<StockTransferRequestLineVM> data, int count)> GetStockTransferRequestLines(string reference, DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetStockTransferRequests(DataGridIntent intent);
}
