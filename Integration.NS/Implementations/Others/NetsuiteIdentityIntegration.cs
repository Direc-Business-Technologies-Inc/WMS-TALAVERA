using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction;
using Integration.NS.Services;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others
{
    public class NetsuiteIdentityIntegration(SuiteQLQueryBuilderFactoryService factory, INetSuiteApiClientService netsuiteApi) : INetsuiteIdentityIntegration
    {
        public async Task<NetsuiteIdentityDTO?> GetNetsuiteIdentityAsync(int netsuiteEmployeeId)
        {
            var query = factory.Create()
                .Select(
                    ("e.id", nameof(NetsuiteIdentityDTO.EmployeeID)),
                    ("e.firstName", nameof(NetsuiteIdentityDTO.EmployeeFirstName)),
                    ("e.lastName", nameof(NetsuiteIdentityDTO.EmployeeLastName)),
                    ("e.subsidiary", nameof(NetsuiteIdentityDTO.SubsidiaryID)),
                    ("s.name", nameof(NetsuiteIdentityDTO.SubsidiaryName))
                ).From("employee e")
                .Join("subsidiary s", "e.subsidiary = s.id")
                .WithFilters(
                    DataGridFilterUtilities.Equal("e.id", netsuiteEmployeeId)
                ).Build();

            var response = await netsuiteApi.ExecuteSuiteQLQuery<NetsuiteIdentityDTO>(query.Query);

            return response.items.FirstOrDefault();
        }
    }
}
