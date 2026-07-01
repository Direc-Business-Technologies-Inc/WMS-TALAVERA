using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Services;
using Shared.Entities;

namespace Integration.NS.Implementations.Others;

public class EmployeeIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory)
    : IEmployeeIntegration
{
    public async Task<(IEnumerable<EmployeeNsDTO>, int count)> GetEmployeesListAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", "NsId"),
                ("entityid", "EmployeeCode"),
                ("firstname", "FirstName"),
                ("lastname", "LastName"),
                ("department", "NsDepartmentId"),
                ("BUILTIN.DF(department)", "DepartmentName"),
                ("subsidiary", "NsSubsidiaryId"),
                ("BUILTIN.DF(subsidiary)", "SubsidiaryName")
            )
            .From("employee")
            .WithFilter(new AppFilterDescriptor
            {
                Property = "BUILTIN.DF(custentity_dbti_other_roles)",
                ComparisonOperator = ComparisonOperatorEnum.Contains,
                Value = "WMS"
            }).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<EmployeeNsDTO>(query.Query, limit: query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }
}
