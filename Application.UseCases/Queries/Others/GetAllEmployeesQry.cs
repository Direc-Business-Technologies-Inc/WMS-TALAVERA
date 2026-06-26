using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetAllEmployeesQry(DataGridIntent Intent) : IRequest<(IEnumerable<EmployeeNsDTO> Data, int Count)>;

public class GetAllEmployeesQryHandler(IEmployeeIntegration empReadRepo) : IRequestHandler<GetAllEmployeesQry, (IEnumerable<EmployeeNsDTO> Data, int Count)>
{
    public async Task<(IEnumerable<EmployeeNsDTO> Data, int Count)> Handle(GetAllEmployeesQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<EmployeeNsDTO> Data, int Count) = await empReadRepo.GetEmployeesListAsync(request.Intent);
        var x = Data.Adapt<IEnumerable<EmployeeNsDTO>>();
        return (x, Count);
    }
}