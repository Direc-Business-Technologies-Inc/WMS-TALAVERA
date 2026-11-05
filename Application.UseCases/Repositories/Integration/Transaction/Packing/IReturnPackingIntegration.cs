using Application.DataTransferObjects.Transactions.Packing;
using Application.DataTransferObjects.Transactions.Packing.Returns;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IReturnPackingIntegration
{
    Task<(IEnumerable<ReturnsDataGridDTO> Data, int Count)> GetPackingReturnsList(DataGridIntent intent, int subsidiaryId);
    Task<ReturnsInfoDTO?> GetPackingReturn(string id);
    Task<(IEnumerable<ReturnsLineDTO> Data, int Count)> GetPackingReturnLines(string id, DataGridIntent intent);

    /// <summary>
    /// Resolves trip-ticket exemption for a return by its transaction reference (tranid),
    /// independent of whether the order still matches the packing filters.
    /// </summary>
    /// <param name="tranid">Order reference number.</param>
    /// <returns>
    /// True/false when the order exists. Null when no such order could be found, which callers
    /// must treat as an unresolved lookup rather than as non-exempt.
    /// </returns>
    Task<bool?> GetTripTicketExemption(string tranid);

    Task<(IEnumerable<PackedItemFulfillmentDTO> Data, int Count)> GetPackedItemFulfillments(DataGridIntent intent);
}
