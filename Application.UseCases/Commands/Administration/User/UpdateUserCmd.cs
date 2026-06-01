using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Administration.User.Management;
using Domain.ValueObjects.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Administration.User;

public record UpdateUserCmd(UserDTO User) : ITransactionalRequest<bool>;

public class UpdateUserCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo)
    : IRequestHandler<UpdateUserCmd, bool>
{
    public async Task<bool> Handle(UpdateUserCmd request, CancellationToken cancellationToken)
    {
        if (await appReadRepo.ExistsAsync<UserDEM>(x => x.Account.UserName.Value.ToLower() == request.User.Account.UserName.Value.ToLower() && x.Id != request.User.Id))
            throw new Exception("Username already in use");
        if (await appReadRepo.ExistsAsync<UserDEM>(x => x.Email.Address.ToLower() == request.User.Email.Address.ToLower() && x.Id != request.User.Id))
            throw new Exception("Email Address already in use");
        UserDEM? dem = await appReadRepo.FirstOrDefaultAsync<UserDEM>(x => x.Id == request.User.Id, true);

        if (dem is null)
            throw new Exception("User not found");

        request.User.Account.HashedPassword = dem.Account.HashedPassword;

        dem.Update(new PersonNameVO(request.User.Name.FirstName,
                                    request.User.Name.MiddleName,
                                    request.User.Name.LastName),
                   new EmailVO(request.User.Email.Address),
                   new AccountVO(new UserNameVO(request.User.Account.UserName.Value),
                                                request.User.Account.HashedPassword,
                                                request.User.Account.Locked,
                                                request.User.Account.LockoutEnabled),
                   request.User.Company,
                   request.User.Role.Id,
                   request.User.PhoneNumber);

        if (request.User.Active)
            dem.Activate();
        else
            dem.Deactivate();

        appCommandRepo.Update(dem);
        return true;
    }
}
