using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
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
    const int TRADE_VENDOR_CATEGORY_ID = 5;
    const int NONTRADE_VENDOR_CATEGORY_ID = 6;
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var query = builderFactory.Create()
            .Select(
                ("v.id", nameof(VendorNSDTO.Id)),
                ("v.entityid", nameof(VendorNSDTO.ReferenceNumber)),
                ("v.companyname", nameof(VendorNSDTO.CompanyName)),
                ("v.fullname", nameof(VendorNSDTO.Name)),
                ("categ.id", nameof(VendorNSDTO.CategoryId)),
                ("categ.name", nameof(VendorNSDTO.CategoryName))
            )
            .From("vendor v")
            .LeftJoin("VendorCategory categ", "v.category = categ.id")
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<VendorNSDTO>(netsuiteService, ct);
        return (result.items.Select(ConvertNSDTO), result.totalResults);
    }
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var query = builderFactory.Create()
            .Select(
                ("v.id", nameof(VendorNSDTO.Id)),
                ("v.entityid", nameof(VendorNSDTO.ReferenceNumber)),
                ("v.companyname", nameof(VendorNSDTO.CompanyName)),
                ("v.fullname", nameof(VendorNSDTO.Name)),
                ("categ.id", nameof(VendorNSDTO.CategoryId)),
                ("categ.name", nameof(VendorNSDTO.CategoryName))
            )
            .From("vendor v")
            .Join("vendorSubsidiaryRelationship vsr", "vsr.entity = v.id")
            .LeftJoin("VendorCategory categ", "v.category = categ.id")
            .WithFilters(DataGridFilterUtilities.Equal("vsr.subsidiary", subsidiary))
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<VendorNSDTO>(netsuiteService, ct);
        return (result.items.Select(ConvertNSDTO), result.totalResults);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var newIntent = intent.Adapt<DataGridIntent>(); // do not modify original intent
        newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(VendorNSDTO.CategoryId), TRADE_VENDOR_CATEGORY_ID));

        return GetVendorsListAsync(newIntent, ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.Add(
            DataGridFilterUtilities.Equal(
                nameof(VendorNSDTO.CategoryId), 
                TRADE_VENDOR_CATEGORY_ID));

        return GetVendorsBySubsidiaryListAsync(newIntent, subsidiary, ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var newIntent = intent.Adapt<DataGridIntent>(); // do not modify original intent
        newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(VendorNSDTO.CategoryId), NONTRADE_VENDOR_CATEGORY_ID));

        return GetVendorsListAsync(newIntent, ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.Add(
            DataGridFilterUtilities.Equal(
                nameof(VendorNSDTO.CategoryId),
                NONTRADE_VENDOR_CATEGORY_ID));

        return GetVendorsBySubsidiaryListAsync(newIntent, subsidiary, ct);
    }

    /// <summary>
    ///     Reads the vendor records for a known set of internal ids. Deliberately
    ///     not paged: the caller passes the handful of vendors on one document.
    /// </summary>
    public async Task<IEnumerable<VendorDTO>> GetVendorsByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var vendorIds = (ids ?? []).Where(id => id > 0).Distinct().ToArray();

        if (vendorIds.Length == 0)
        {
            return [];
        }

        var query = builderFactory.Create()
            .Select(
                ("v.id", nameof(VendorNSDTO.Id)),
                ("v.entityid", nameof(VendorNSDTO.ReferenceNumber)),
                ("v.companyname", nameof(VendorNSDTO.CompanyName)),
                ("v.fullname", nameof(VendorNSDTO.Name))
            )
            .From("vendor v")
            .WithFilters(DataGridFilterUtilities.In("v.id", vendorIds))
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<VendorNSDTO>(query.Query, ct: ct);

        return result.items.Select(ConvertNSDTO);
    }


    public VendorDTO ConvertNSDTO(VendorNSDTO nsdto)
    {
        return nsdto.Adapt(new VendorDTO
        {
            Category = new VendorCategoryDTO
            {
                Name = nsdto.CategoryName,
                Id = nsdto.CategoryId
            }
        });
    }
    public class VendorNSDTO
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
