using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Administration.User;

public record GetAllUsersQry(DataGridIntent Intent) : IRequest<(IEnumerable<UserDataGridDTO> Data, int Count)>;

public class GetAllUsersQryHandler(
    IUserReadRepo userReadRepo)
    : IRequestHandler<GetAllUsersQry, (IEnumerable<UserDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<UserDataGridDTO> Data, int Count)> Handle(GetAllUsersQry request, CancellationToken cancellationToken)
    {
        var dto = await userReadRepo.GetUserTableDetails(request.Intent);

        return dto;

    }
}
