using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Administration.User;

public record GetUserQry(Guid Reference) : IRequest<UserDTO?>;

public class GetUserQryHandler(
    IAppReadRepository appReadRepo)
    : IRequestHandler<GetUserQry, UserDTO?>
{
    public async Task<UserDTO?> Handle(GetUserQry request, CancellationToken cancellationToken)
    {
        UserDEM? userDEM = await appReadRepo.FirstOrDefaultAsync<UserDEM>(x => x.Id == request.Reference);
        
        RoleDEM? roleDEM = null;
        if (userDEM is not null)
            roleDEM = await appReadRepo.FirstOrDefaultAsync<RoleDEM>(x => x.Id == userDEM.RoleId);

        UserDTO? userDTO = userDEM.Adapt<UserDTO>();

        userDTO.Account.HashedPassword = string.Empty;
        userDTO.Role = roleDEM.Adapt<RoleDTO>();

        return userDTO;

    }
}

