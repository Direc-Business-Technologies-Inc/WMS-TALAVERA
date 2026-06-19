using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;

public interface IStockTransferRequestHandler
{
    Task<StockTransferRequestInfoVM?> GetStockTransferRequest(string reference, bool includeLines = true);
    Task<(IEnumerable<StockTransferRequestLineVM> data, int count)> GetStockTransferRequestLines(string reference, DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetTransferOrdersList(DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetInterCompanyTransferOrdersList(DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetReturnsList(DataGridIntent intent);

    Task<bool> CreateStockTransferRequest(StockTransferRequestInfoVM data);
    Task<bool> UpdateStockTransferRequest(StockTransferRequestInfoVM data);
}
