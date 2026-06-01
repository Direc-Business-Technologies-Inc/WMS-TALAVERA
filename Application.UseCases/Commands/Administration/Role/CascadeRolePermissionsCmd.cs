using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using MediatR;

namespace Application.UseCases.Commands.Administration.Role;

public record CascadeRolePermissionsCmd(IEnumerable<RolePermissionDTO> Permissions) : ITransactionalRequest<bool>;

public class CascadeRolePermissionsCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo)
    : IRequestHandler<CascadeRolePermissionsCmd, bool>
{
    public async Task<bool> Handle(CascadeRolePermissionsCmd request, CancellationToken cancellationToken)
    {
        var roleIds = request.Permissions.Select(p => p.RoleId).Distinct().ToList();
        var modulePermissionIds = request.Permissions.Select(p => p.Permission.Id).Distinct().ToList();

        var roles = await appReadRepo.GetListAsync<RoleDEM>(r => roleIds.Contains(r.Id));
        var modulePermissions = await appReadRepo.GetListAsync<ModulePermissionDEM>(mp => modulePermissionIds.Contains(mp.Id), true);

        foreach (var role in roles)
        {
            await appCommandRepo.DeleteManyAsync<RolePermissionDEM>(rp => rp.RoleId == role.Id);

            var permissionsForRole = request.Permissions.Where(p => p.RoleId == role.Id).Select(p => p.Permission.Id).ToList();
            var permissionsToAssign = modulePermissions.Where(mp => permissionsForRole.Contains(mp.Id)).ToList();

            List<RolePermissionDEM> permissionsToAdd = [];

            foreach (var permission in permissionsToAssign)
            {
                permissionsToAdd.Add(RolePermissionDEM.Create(permission.Id, role.Id));
            }

            await appCommandRepo.AddManyAsync<RolePermissionDEM>(permissionsToAdd);

            List<UserDEM> users = await appReadRepo.GetListAsync<UserDEM>(rp => rp.RoleId == role.Id);

            foreach (var user in users)
            {
                await appCommandRepo.DeleteManyAsync<UserPermissionDEM>(up => up.UserId == user.Id);

                List<UserPermissionDEM> userPermissionsToAdd = [];

                foreach (var permission in permissionsToAssign)
                {
                    userPermissionsToAdd.Add(UserPermissionDEM.Create(permission.Id, user.Id));
                }

                await appCommandRepo.AddManyAsync<UserPermissionDEM>(userPermissionsToAdd);
            }

            appCommandRepo.UpdateMany(users);
        }

        appCommandRepo.UpdateMany(roles);
        return true;
    }
}
