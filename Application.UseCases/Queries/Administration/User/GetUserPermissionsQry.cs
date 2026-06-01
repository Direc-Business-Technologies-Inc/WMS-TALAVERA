using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;

namespace Application.UseCases.Queries.Administration.User;

public record GetUserPermissionsQry(Guid UserId) : IRequest<IEnumerable<UserPermissionDTO>>;

public class GetUserPermissionsQryHandler(
    IUserReadRepo userReadRepo)
    : IRequestHandler<GetUserPermissionsQry, IEnumerable<UserPermissionDTO>>
{
    public async Task<IEnumerable<UserPermissionDTO>> Handle(GetUserPermissionsQry request, CancellationToken cancellationToken)
    {
        IEnumerable<UserPermissionDTO> permissions = await userReadRepo.GetUserPermissions(request.UserId);
        return permissions;
    }
}
