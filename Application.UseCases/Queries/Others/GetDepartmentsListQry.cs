using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others
{
    public record GetDepartmentsListQry(DataGridIntent Intent) : IRequest<(IEnumerable<DepartmentDTO>, int)>;

    public class GetDepartmentsListQryHandler(
        IDepartmentIntegration departmentIntegration )
        : IRequestHandler<GetDepartmentsListQry, (IEnumerable<DepartmentDTO>, int)>
    {
        public Task<(IEnumerable<DepartmentDTO>, int)> Handle(GetDepartmentsListQry request, CancellationToken cancellationToken)
        {
            return departmentIntegration.GetDepartmentsList(request.Intent);
        }
    }
}
