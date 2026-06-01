using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Shared.Entities;

namespace Integration.SAP.Implementations.Others;

public class SchoolYearIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : ISchoolYearIntegration
{
    public async Task<(IEnumerable<SchoolYearSAPDTO> Data, int Count)> GetAllSchoolYear(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "Code", "Code" },
                { "Name", "Name" },
                { "YearFrom", "U_YearFrom" },
                { "YearTo", "U_YearTo" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "Name",
                Direction = SortDirectionEnum.Ascending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_SchoolYear", out string qry, out bool found);
        if (!found)
            throw new Exception("Base query for getting School Years not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<SchoolYearSAPDTO> data = await SLActions.RawQueryAsync<SchoolYearSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (data, rowCount?.Count ?? data.Count);
    }
}
