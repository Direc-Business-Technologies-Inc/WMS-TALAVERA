using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Bases;
using DataCipher;
using Domain.Entities.Administration.User.Management;
using MediatR;

namespace Application.UseCases.Commands.Administration.User;

public record UpdateUserPasswordCmd(UserPasswordDTO Password) : ITransactionalRequest<bool>;

public class UpdateUserPasswordCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo)
    : IRequestHandler<UpdateUserPasswordCmd, bool>
{
    public async Task<bool> Handle(UpdateUserPasswordCmd request, CancellationToken cancellationToken)
    {
        UserDEM? dem = await appReadRepo.FirstOrDefaultAsync<UserDEM>(x => x.Id == request.Password.UserId, true);

        if (dem is null)
            throw new Exception("User not found");

        string hashedPassword = Encryption.Encrypt(request.Password.Password);

        dem.ChangePassword(hashedPassword);

        appCommandRepo.Update(dem);

        return true;
    }
}
