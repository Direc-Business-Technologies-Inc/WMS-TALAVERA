using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;

namespace Application.UseCases.Queries.Administration.Role;

public record GetRolePermissionsQry(Guid RoleId) : IRequest<IEnumerable<RolePermissionDTO>>;

public class GetRolePermissionsQryHandler(
    IRoleReadRepo RoleReadRepo)
    : IRequestHandler<GetRolePermissionsQry, IEnumerable<RolePermissionDTO>>
{
    public async Task<IEnumerable<RolePermissionDTO>> Handle(GetRolePermissionsQry request, CancellationToken cancellationToken)
    {
        IEnumerable<RolePermissionDTO> permissions = await RoleReadRepo.GetRolePermissions(request.RoleId);
        return permissions;
    }
}
