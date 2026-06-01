using Application.DataTransferObjects.Others.SAP;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IBusinessPartnerIntegration
{
    Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetCustomersAsync(DataGridIntent intent);
    Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetVendorsAsync(DataGridIntent intent);
    Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetAllAsync(DataGridIntent intent);
}
