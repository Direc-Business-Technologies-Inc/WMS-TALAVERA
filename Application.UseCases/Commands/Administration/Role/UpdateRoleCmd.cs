using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Role;
using MediatR;

namespace Application.UseCases.Commands.Administration.Role;

public record UpdateRoleCmd(RoleDTO Role) : ITransactionalRequest<bool>;

public class UpdateRoleCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo)
    : IRequestHandler<UpdateRoleCmd, bool>
{
    public async Task<bool> Handle(UpdateRoleCmd request, CancellationToken cancellationToken)
    {
        RoleDEM? dem = await appReadRepo.FirstOrDefaultAsync<RoleDEM>(x => x.Id == request.Role.Id, true);

        if (dem is null)
            throw new Exception("Role not found");

        if (request.Role.Active != dem.Active)
            dem.SetActiveStatus(request.Role.Active);
        if (!string.IsNullOrEmpty(request.Role.Code) && request.Role.Code != dem.Code)
            dem.ChangeCode(request.Role.Code);
        if (!string.IsNullOrEmpty(request.Role.Name) && request.Role.Name != dem.Name)
            dem.ChangeName(request.Role.Name);

        appCommandRepo.Update(dem);
        return true;
    }
}