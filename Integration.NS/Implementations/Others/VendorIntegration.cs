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
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent)
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
            .WithDatagridIntent(EscapeSearchLiterals(intent))
            .Build();

        var result = await query.ExecuteWithPaging<VendorNSDTO>(netsuiteService);
        return (result.items.Select(ConvertNSDTO), result.totalResults);
    }
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary)
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

        var result = await query.ExecuteWithPaging<VendorNSDTO>(netsuiteService);
        return (result.items.Select(ConvertNSDTO), result.totalResults);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent)
    {
        var newIntent = intent.Adapt<DataGridIntent>(); // do not modify original intent
        newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(VendorNSDTO.CategoryId), TRADE_VENDOR_CATEGORY_ID));

        return GetVendorsListAsync(newIntent);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary)
    {
        var newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.Add(
            DataGridFilterUtilities.Equal(
                nameof(VendorNSDTO.CategoryId), 
                TRADE_VENDOR_CATEGORY_ID));

        return GetVendorsBySubsidiaryListAsync(newIntent, subsidiary);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent)
    {
        var newIntent = intent.Adapt<DataGridIntent>(); // do not modify original intent
        newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(VendorNSDTO.CategoryId), NONTRADE_VENDOR_CATEGORY_ID));

        return GetVendorsListAsync(newIntent);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary)
    {
        var newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.Add(
            DataGridFilterUtilities.Equal(
                nameof(VendorNSDTO.CategoryId),
                NONTRADE_VENDOR_CATEGORY_ID));

        return GetVendorsBySubsidiaryListAsync(newIntent, subsidiary);
    }


    private static DataGridIntent EscapeSearchLiterals(DataGridIntent intent)
    {
        if (intent.Filters.Count == 0)
        {
            return intent;
        }

        var cloned = new DataGridIntent
        {
            Skip = intent.Skip,
            Take = intent.Take,
            Sorts = intent.Sorts.ToList(),
            Filters = intent.Filters.Select(EscapeFilter).ToList()
        };

        return cloned;
    }

    private static AppFilterDescriptor EscapeFilter(AppFilterDescriptor filter)
    {
        if (filter.Filters.Count > 0)
        {
            return new AppFilterDescriptor
            {
                LogicalOperator = filter.LogicalOperator,
                Property = filter.Property,
                Value = filter.Value,
                FilterValueType = filter.FilterValueType,
                ComparisonOperator = filter.ComparisonOperator,
                Filters = filter.Filters.Select(EscapeFilter).ToList()
            };
        }

        if (filter.Value is string s && !string.IsNullOrEmpty(s))
        {
            var escaped = s.Replace("'", "''");
            if (escaped != s)
            {
                return new AppFilterDescriptor
                {
                    LogicalOperator = filter.LogicalOperator,
                    Property = filter.Property,
                    Value = escaped,
                    FilterValueType = filter.FilterValueType,
                    ComparisonOperator = filter.ComparisonOperator,
                    Filters = filter.Filters.ToList()
                };
            }
        }

        return filter;
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
