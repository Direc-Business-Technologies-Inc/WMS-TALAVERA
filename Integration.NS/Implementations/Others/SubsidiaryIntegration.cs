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

public class SubsidiaryIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : ISubsidiaryIntegration
{

    public async Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(SubsidiaryDTO.Id)),
                ("externalid", nameof(SubsidiaryDTO.SubsidiaryNumber)),
                ("BUILTIN.DF(mainaddress)", nameof(SubsidiaryDTO.Address)),
                ("name", nameof(SubsidiaryDTO.Name)),
                ("email", nameof(SubsidiaryDTO.Email))
            )
            .From("subsidiary")
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<SubsidiaryDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }

    public async Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId)
    {
        var query = builderFactory.Create()
            .Select(
                ("s.id", nameof(SubsidiaryDTO.Id)),
                ("s.externalid", nameof(SubsidiaryDTO.SubsidiaryNumber)),
                ("s.BUILTIN.DF(mainaddress)", nameof(SubsidiaryDTO.Address)),
                ("s.name", nameof(SubsidiaryDTO.Name)),
                ("s.email", nameof(SubsidiaryDTO.Email))
            )
            .From("subsidiary s")
            .Join("customersubsidiaryrelationship csr", "s.id = csr.subsidiary")
            .WithDatagridIntent(intent)
            .WithFilters(DataGridFilterUtilities.Equal("csr.entity", customerId))
            .Build();

        var result = await query.ExecuteWithPaging<SubsidiaryDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }

    public async Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId)
    {
        var query = builderFactory.Create()
            .Select(
                ("s.id", nameof(SubsidiaryDTO.Id)),
                ("s.externalid", nameof(SubsidiaryDTO.SubsidiaryNumber)),
                ("s.BUILTIN.DF(mainaddress)", nameof(SubsidiaryDTO.Address)),
                ("s.name", nameof(SubsidiaryDTO.Name)),
                ("s.email", nameof(SubsidiaryDTO.Email))
            )
            .From("subsidiary s")
            .Join("vendorSubsidiaryRelationship vsr", "vsr.subsidiary = s.id")
            .WithFilters(DataGridFilterUtilities.Equal("vsr.entity", vendorId))
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<SubsidiaryDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
}
