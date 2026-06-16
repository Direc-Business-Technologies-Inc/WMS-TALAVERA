using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;

public interface IStockTransferRequestIntegration
{
    Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetTransferOrderList(DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetIntercompanyTransferOrderList(DataGridIntent intent);
    Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetReturnsList(DataGridIntent intent);
    Task<StockTransferRequestInfoDTO?> GetStockTransferRequest(string id);
    Task<IEnumerable<StockTransferRequestLineDTO>?> GetStockTransferRequestLines(string id);
    Task<bool> CreateStockTransferRequest(StockTransferRequestInfoDTO dto);
    Task<bool> UpdateStockTransferRequest(StockTransferRequestInfoDTO dto);


}
