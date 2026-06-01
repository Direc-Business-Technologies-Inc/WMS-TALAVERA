using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Shared.Entities;

namespace Integration.SAP.Implementations.Others;

public class BusinessPartnerIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IBusinessPartnerIntegration
{
    public async Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetAllAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "CardCode", "T0.CardCode" },
                { "CardName", "T0.CardName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "CardName",
                Direction = SortDirectionEnum.Ascending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_AllBps", out string qry, out bool found);
        if (!found)
            throw new Exception("Base query for getting all Business Partners not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<BusinessPartnerSAPDTO> data = await SLActions.RawQueryAsync<BusinessPartnerSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (data, rowCount?.Count ?? data.Count);
    }

    public async Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetCustomersAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "CardCode", "T0.CardCode" },
                { "CardName", "T0.CardName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "CardName",
                Direction = SortDirectionEnum.Ascending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_Customers", out string qry, out bool found);
        if (!found)
            throw new Exception("Base query for getting all Customers not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<BusinessPartnerSAPDTO> data = await SLActions.RawQueryAsync<BusinessPartnerSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (data, rowCount?.Count ?? data.Count);
    }

    public async Task<(IEnumerable<BusinessPartnerSAPDTO> Data, int Count)> GetVendorsAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "CardCode", "T0.CardCode" },
                { "CardName", "T0.CardName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "CardName",
                Direction = SortDirectionEnum.Ascending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_Vendors", out string qry, out bool found);
        if (!found)
            throw new Exception("Base query for getting all Vendors not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<BusinessPartnerSAPDTO> data = await SLActions.RawQueryAsync<BusinessPartnerSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (data, rowCount?.Count ?? data.Count);
    }
}
