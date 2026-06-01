using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Shared.Entities;

namespace Integration.NS.Implementations.Others;

public class BusinessPartnerIntegration : IBusinessPartnerIntegration
{
    public Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetAllAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetCustomersAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetVendorsAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }
}
