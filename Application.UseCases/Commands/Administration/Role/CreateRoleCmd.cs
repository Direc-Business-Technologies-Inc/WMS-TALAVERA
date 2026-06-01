using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using MediatR;

namespace Application.UseCases.Commands.Administration.Role;

public record CreateRoleCmd(RoleDTO Role) : ITransactionalRequest<bool>;

public class CrateRoleCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo)
    : IRequestHandler<CreateRoleCmd, bool>
{
    public async Task<bool> Handle(CreateRoleCmd request, CancellationToken cancellationToken)
    {
        RoleDEM dem = RoleDEM.Create(request.Role.Code, request.Role.Name);

        ModuleDEM? dashboardModule = await appReadRepo.FirstOrDefaultAsync<ModuleDEM>(x => x.Code == "ODSB");
        if (dashboardModule is not null)
        {
            List<ModulePermissionDEM> dashboardPermissions = await appReadRepo.GetListAsync<ModulePermissionDEM>(x => x.ModuleReference == dashboardModule.Id);

            foreach (var dp in dashboardPermissions)
                dem.AddPermission(RolePermissionDEM.Create(dp.Id, dem.Id));

        }

        await appCommandRepo.AddAsync(dem);
        return true;
    }
}

