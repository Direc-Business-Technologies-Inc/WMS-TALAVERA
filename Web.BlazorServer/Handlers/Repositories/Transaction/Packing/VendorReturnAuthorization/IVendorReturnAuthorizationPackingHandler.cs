using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;

public interface IVendorReturnAuthorizationPackingHandler
{
    Task<(IEnumerable<VendorReturnAuthorizationPackingDataGridVM> Data, int Count)> GetVendorReturnAuthorizationsList(DataGridIntent intent);
    Task<VendorReturnAuthorizationInfoPackingVM?> GetPackingVendorReturnAuthorization(string reference);
    Task<(IEnumerable<VendorReturnAuthorizationLinePackingVM> Data, int Count)> GetPackingVendorReturnAuthorizationLines(string reference, DataGridIntent intent);
}
