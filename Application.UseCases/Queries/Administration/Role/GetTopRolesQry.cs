using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.Administration.User;
using Domain.Entities.Administration.User.Role;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Administration.Role;

public record GetTopRolesQry : IRequest<IEnumerable<RoleDTO>>;

public class GetTopRolesQryHandler(
   IRoleReadRepo roleReadRepo)
    : IRequestHandler<GetTopRolesQry, IEnumerable<RoleDTO>>
{

    public async Task<IEnumerable<RoleDTO>> Handle(GetTopRolesQry request, CancellationToken cancellationToken)
    {
        DataGridIntent intent = new()
        {
            Take = 10,
            Filters =
            [
                new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(RoleDTO.Active),
                    Value = true,
                    ComparisonOperator = ComparisonOperatorEnum.Equals,
                    FilterValueType = FilterValueTypeEnum.Boolean
                }
            ]
        };

        var dto = await roleReadRepo.GetRoleTableDetails(intent);

        return dto.data;
    }
}