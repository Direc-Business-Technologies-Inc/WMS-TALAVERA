using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class DepartmentIntegration(
    INetSuiteApiClientService netsuiteService, 
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : IDepartmentIntegration
{
    public async Task<(IEnumerable<DepartmentDTO>, int)> GetDepartmentsList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("d.name", nameof(DepartmentDTO.Name)),
                ("d.id", nameof(DepartmentDTO.Id)),
                ("d.custrecord_dbti_department_code", nameof(DepartmentDTO.Code))
            )
            .From("department d")
            .Join("DepartmentSubsidiaryMap dsm", "dsm.department = d.id")
            .WithDatagridIntent(intent)
            .WithSubsidiaries(httpContextAccessor, "dsm")
            .Build();

        var response = await query.ExecuteWithPaging<DepartmentDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }
}
