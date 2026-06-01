using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Role;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Administration.Role;

public record GetRoleQry(Guid Reference) : IRequest<RoleDTO?>;

public class GetRoleQryHandler(
    IAppReadRepository appReadRepo)
    : IRequestHandler<GetRoleQry, RoleDTO?>
{
    public async Task<RoleDTO?> Handle(GetRoleQry request, CancellationToken cancellationToken)
    {
        var dem = await appReadRepo.FirstOrDefaultAsync<RoleDEM>(x => x.Id == request.Reference);

        return dem.Adapt<RoleDTO>();

    }
}
