using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.System.Security;
using Application.UseCases.Repositories.Bases;
using DataCipher;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Providers;
using Mapster;
using MediatR;

namespace Application.UseCases.Commands.System.Authentication;

public record LoginCmd(AuthenticationPayloadDTO Login) : ITransactionalRequest<AuthenticationResponseDTO>;

public class LoginCmdHandler(
    IAppReadRepository appRead,
    IAppCommandRepository appCommand)
    : IRequestHandler<LoginCmd, AuthenticationResponseDTO>
{
    public async Task<AuthenticationResponseDTO> Handle(LoginCmd request, CancellationToken cancellationToken)
    {
        if (!await appRead.ExistsAsync<UserDEM>(x => x.Account.UserName.Value == request.Login.UserName))
            return AuthenticationResponseDTO.Fail("User not found");

        UserDEM? user = await appRead.FirstOrDefaultAsync<UserDEM>(x => x.Account.UserName.Value == request.Login.UserName, true);
        if (user is null)
            return AuthenticationResponseDTO.Fail("User not found");
        LoginDEM? login = await appRead.FirstOrDefaultAsync<LoginDEM>(x => x.AccountId == user.Id && x.LoginDate.Date == DateTimeProvider.UtcNow.Date, true);

        if (!user.Active)
            return AuthenticationResponseDTO.Fail("User Account is marked as Inactive");
        if (user.Account.LockoutEnabled && user.Account.Locked)
            return AuthenticationResponseDTO.Fail("User Account is Locked");

        login ??= LoginDEM.Create(false, user.Id);
        user.AddNewLogin(login);

        if (!Encryption.Decrypt(user.Account.HashedPassword).Equals(request.Login.Password))
        {
            login.NewAttempt();
            if (login.AttemptCount > 3)
                user.Account.Lock();

            appCommand.Update(user);

            return AuthenticationResponseDTO.Fail("Login Failed! Please check your Username or Password");
        }
        else
        {
            login.ResetAttempts();
            user.Account.Unlock();
        }

        RoleDEM? role = await appRead.FirstOrDefaultAsync<RoleDEM>(x => x.Id == user.RoleId, false);
        if (role is null)
            return AuthenticationResponseDTO.Fail("User Role not found");

        UserDTO dto = user.Adapt<UserDTO>();
        dto.Role = role.Adapt<RoleDTO>();

        appCommand.Update(user);
        return AuthenticationResponseDTO.Success("Authentication Success! Redirecting you now, please wait ...", dto);
    }
}
