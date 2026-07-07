using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IVendorReturnAuthorizationPackingIntegration
{
    Task<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)> GetPackingVendorReturnAuthorizationsList(DataGridIntent intent, int subsidiaryId);
    Task<VendorReturnAuthorizationInfoDTO?> GetPackingVendorReturnAuthorization(string id);
    Task<(IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)> GetPackingVendorReturnAuthorizationLines(string id, DataGridIntent intent);
}
