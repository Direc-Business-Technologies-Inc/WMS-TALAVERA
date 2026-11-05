using Application.DataTransferObjects.Transactions.Packing.STR;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IStockTransferRequestPackingIntegration
{
    Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> GetPackingStockTransferRequestList(DataGridIntent intent, int subsidiaryId);
    Task<StockTransferRequestInfoPackingDTO?> GetPackingStockTransferRequest(string id);
    Task<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)> GetPackingStockTransferRequestLines(string id, DataGridIntent intent);

    /// <summary>
    /// Resolves trip-ticket exemption for an order by its transaction reference (tranid),
    /// independent of whether the order still matches the packing filters.
    /// </summary>
    /// <param name="tranid">Order reference number.</param>
    /// <returns>
    /// True/false when the order exists. Null when no such order could be found, which callers
    /// must treat as an unresolved lookup rather than as non-exempt.
    /// </returns>
    Task<bool?> GetTripTicketExemption(string tranid);
}
