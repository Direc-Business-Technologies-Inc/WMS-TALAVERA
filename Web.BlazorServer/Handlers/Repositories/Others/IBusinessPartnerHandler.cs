using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IBusinessPartnerHandler
{
    Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetCustomersAsync(DataGridIntent intent);
    Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetVendorsAsync(DataGridIntent intent);
    Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetAllAsync(DataGridIntent intent);
}
