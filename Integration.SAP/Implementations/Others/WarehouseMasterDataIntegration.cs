using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Shared.Entities;

namespace Integration.SAP.Implementations.Others;

public class WarehouseMasterDataIntegration (
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IWarehouseMasterDataIntegration
{
    public async Task<(IEnumerable<WarehouseSelectionSAPDTO> Data, int Count)> GetWarehouseAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "WhsCode", "T0.WhsCode" },
                { "WhsName", "T0.WhsName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "WhsName",
                Direction = SortDirectionEnum.Ascending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_WarehouseSelection", out string qry, out bool found);
        if (!found)
            throw new Exception("Base query for getting warehouses not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<WarehouseSelectionSAPDTO> data = await SLActions.RawQueryAsync<WarehouseSelectionSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (data, rowCount?.Count ?? data.Count);
    }
}
