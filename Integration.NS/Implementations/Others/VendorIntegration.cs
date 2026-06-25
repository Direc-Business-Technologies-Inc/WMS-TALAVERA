using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
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
            .From("vendor")
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<VendorDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary)
    {
        var query = builderFactory.Create()
            .Select(
                ("v.id", nameof(VendorDTO.Id)),
                ("v.entityid", nameof(VendorDTO.ReferenceNumber)),
                ("v.companyname", nameof(VendorDTO.CompanyName)),
                ("v.fullname", nameof(VendorDTO.Name))
            )
            .From("vendor v")
            .Join("vendorSubsidiaryRelationship vsr", "vsr.entity = v.id")
            .WithFilters(DataGridFilterUtilities.Equal("vsr.subsidiary", subsidiary))
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<VendorDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
}
