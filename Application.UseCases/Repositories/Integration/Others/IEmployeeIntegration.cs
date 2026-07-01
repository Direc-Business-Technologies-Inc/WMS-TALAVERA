using Application.DataTransferObjects.Others;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IEmployeeIntegration
{
    public Task<(IEnumerable<EmployeeNsDTO>, int count)> GetEmployeesListAsync(DataGridIntent intent);
}
