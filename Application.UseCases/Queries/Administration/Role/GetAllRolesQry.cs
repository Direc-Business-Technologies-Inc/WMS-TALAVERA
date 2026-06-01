using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Administration.Role;

public record GetAllRolesQry(DataGridIntent Intent) : IRequest<(IEnumerable<RoleDTO> Data, int Count)>;

public class GetAllRolesQryHandler(
    IRoleReadRepo roleReadRepo) 
    : IRequestHandler<GetAllRolesQry, (IEnumerable<RoleDTO> Data, int Count)>
{
    public async Task<(IEnumerable<RoleDTO> Data, int Count)> Handle(GetAllRolesQry request, CancellationToken cancellationToken)
    {
        var dto = await roleReadRepo.GetRoleTableDetails(request.Intent);

        return dto;
    }
}
