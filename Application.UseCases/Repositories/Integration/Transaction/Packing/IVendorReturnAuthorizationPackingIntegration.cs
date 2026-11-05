using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IVendorReturnAuthorizationPackingIntegration
{
    Task<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)> GetPackingVendorReturnAuthorizationsList(DataGridIntent intent, int subsidiaryId);
    Task<VendorReturnAuthorizationInfoDTO?> GetPackingVendorReturnAuthorization(string id);
    Task<(IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)> GetPackingVendorReturnAuthorizationLines(string id, DataGridIntent intent);

    /// <summary>
    /// Resolves trip-ticket exemption for a vendor return authorization by its transaction
    /// reference (tranid), independent of whether the order still matches the packing filters.
    /// </summary>
    /// <param name="tranid">Order reference number.</param>
    /// <returns>
    /// True/false when the order exists. Null when no such order could be found, which callers
    /// must treat as an unresolved lookup rather than as non-exempt.
    /// </returns>
    Task<bool?> GetTripTicketExemption(string tranid);
}
