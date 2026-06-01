using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.System;
using MediatR;

namespace Application.UseCases.Commands.Administration.User;

public record UpdateUserPermissionsCmd(IEnumerable<UserPermissionDTO> Permissions) : ITransactionalRequest<bool>;

public class UpdateUserPermissionsCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo)
    : IRequestHandler<UpdateUserPermissionsCmd, bool>
{
    public async Task<bool> Handle(UpdateUserPermissionsCmd request, CancellationToken cancellationToken)
    {
        var userIds = request.Permissions.Select(p => p.UserId).Distinct().ToList();
        var modulePermissionIds = request.Permissions.Select(p => p.Permission.Id).Distinct().ToList();

        var users = await appReadRepo.GetListAsync<UserDEM>(u => userIds.Contains(u.Id), true);
        var modulePermissions = await appReadRepo.GetListAsync<ModulePermissionDEM>(mp => modulePermissionIds.Contains(mp.Id), true);

        foreach (var user in users)
        {
            await appCommandRepo.DeleteManyAsync<UserPermissionDEM>(up => up.UserId == user.Id);

            var permissionsForUser = request.Permissions.Where(p => p.UserId == user.Id).Select(p => p.Permission.Id).ToList();
            var permissionsToAssign = modulePermissions.Where(mp => permissionsForUser.Contains(mp.Id)).ToList();

            List<UserPermissionDEM> permissionsToAdd = [];

            foreach (var permission in permissionsToAssign)
            {
                var userPermission = UserPermissionDEM.Create(permission.Id, user.Id);
                permissionsToAdd.Add(userPermission);
            }

            await appCommandRepo.AddManyAsync<UserPermissionDEM>(permissionsToAdd);
        }

        appCommandRepo.UpdateMany(users);
        return true;
    }
}
