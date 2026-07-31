using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class DepartmentHandler(ISender sender) : IDepartmentHandler
{
    public async Task<(IEnumerable<DepartmentVM> Data, int Count)> GetDepartmentsListAsync(DataGridIntent intent)
    {
        GetDepartmentsListQry qry = new(intent);

        var response = await sender.Send(qry);

        return (response.Item1.Adapt<IEnumerable<DepartmentVM>>(), response.Item2);
    }
}
