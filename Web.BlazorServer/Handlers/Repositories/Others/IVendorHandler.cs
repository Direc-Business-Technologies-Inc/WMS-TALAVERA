using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IVendorHandler
{
    Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListAsync(DataGridIntent intent);
    Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId);
}
