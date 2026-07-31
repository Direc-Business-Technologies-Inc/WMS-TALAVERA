using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class SubsidiaryIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContext,
    SuiteQLQueryBuilderFactoryService builderFactory) : ISubsidiaryIntegration
{

    public async Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("id", nameof(SubsidiaryDTO.Id)),
                ("externalid", nameof(SubsidiaryDTO.SubsidiaryNumber)),
                ("BUILTIN.DF(mainaddress)", nameof(SubsidiaryDTO.Address)),
                ("name", nameof(SubsidiaryDTO.Name)),
                ("email", nameof(SubsidiaryDTO.Email))
            )
            .From("subsidiary")
            .WithDatagridIntent(intent);


        string? claimValue = httpContext.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue is not null)
        {
            var allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];
            if (allowedSubsidiaries.Any())
            {
                // show user subsidiaries first
                builder.Sorts.Add(
                    DataGridSortUtilities.Descending(
                        $"CASE WHEN id IN ({string.Join(",", allowedSubsidiaries)}) THEN 1 ELSE 0 END"
                ));
            }
        }

        var query = builder.Build();

        var result = await query.ExecuteWithPaging<SubsidiaryDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }

    public async Task<SubsidiaryDTO?> GetSubsidiaryAsync(int subsidiaryid)
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
            .WithFilters(
                DataGridFilterUtilities.Equal(nameof(SubsidiaryDTO.Id), subsidiaryid)
             )
            .Build();

        var result = await query.ExecuteWithPaging<SubsidiaryDTO>(netsuiteService);
        return result.items.FirstOrDefault();
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

    public async Task<IEnumerable<SubsidiaryDTO>> GetChildSubsidiariesAsync(int subsidiaryId)
    {
        List<SubsidiaryDTO> result = [];
        List<int> search = [subsidiaryId];
        HashSet<int> searchmemo = new();
        HashSet<int> addmemo = new();

        while (search.Count > 0)
        {
            var tasks = search.Select(_getChildSubsidiaries);
            var results = await Task.WhenAll(tasks);

            search.ForEach(x => searchmemo.Add(x));
            search.Clear();

            foreach (var list in results) {
                foreach (var item in list) {
                    if (addmemo.Contains(item.Id)) continue;

                    result.Add(item);
                    addmemo.Add(item.Id);

                    if (searchmemo.Contains(item.Id)) continue;

                    search.Add(item.Id);
                }
            }
        }

        return result;
    }

    private async Task<IEnumerable<SubsidiaryDTO>> _getChildSubsidiaries(int subsidiaryId)
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
            .WithFilters(DataGridFilterUtilities.Equal("parent", subsidiaryId))
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<SubsidiaryDTO>(query.Query);
        return result.items;
    }
}
