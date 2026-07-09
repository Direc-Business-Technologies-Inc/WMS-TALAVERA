using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class CustomerIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : ICustomerIntegration
{
    public async Task<(IEnumerable<CustomerDTO> Data, int Count)> GetCustomersListAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(VendorDTO.Id)),
                ("entityid", nameof(VendorDTO.ReferenceNumber)),
                ("companyname", nameof(VendorDTO.CompanyName)),
                ("fullname", nameof(VendorDTO.Name))
            )
            .From("customer")
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<CustomerDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }

}
