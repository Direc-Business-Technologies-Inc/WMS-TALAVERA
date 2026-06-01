using Application.DataTransferObjects.System.Modules;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;

namespace Application.UseCases.Queries.System.Authentication;

public record GetUserModulePermissionsQry(Guid UserId) : IRequest<IEnumerable<ModulePermissionDTO>>;

public class GetUserModulePermissionsQryHandler(
    IUserReadRepo userRead) 
    : IRequestHandler<GetUserModulePermissionsQry, IEnumerable<ModulePermissionDTO>>
{
    public async Task<IEnumerable<ModulePermissionDTO>> Handle(GetUserModulePermissionsQry request, CancellationToken cancellationToken)
    {
        var data = await userRead.GetUserModulePermissions(request.UserId);

        return data;
    }
}