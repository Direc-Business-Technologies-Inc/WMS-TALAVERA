using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Services;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class VendorIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IVendorIntegration
{
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(VendorDTO.Id)),
                ("entityid", nameof(VendorDTO.ReferenceNumber)),
                ("companyname", nameof(VendorDTO.CompanyName)),
                ("fullname", nameof(VendorDTO.Name))
            )
            .From("location")
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<VendorDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.totalResults);
    }
}
