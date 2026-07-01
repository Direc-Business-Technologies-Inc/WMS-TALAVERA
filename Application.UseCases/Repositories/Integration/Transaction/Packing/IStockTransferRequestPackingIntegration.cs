using Application.DataTransferObjects.Transactions.Packing.STR;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IStockTransferRequestPackingIntegration
{
    Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> GetPackingStockTransferRequestList(DataGridIntent intent);
    Task<StockTransferRequestInfoPackingDTO?> GetPackingStockTransferRequest(string id);
    Task<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)> GetPackingStockTransferRequestLines(string id, DataGridIntent intent);
}
