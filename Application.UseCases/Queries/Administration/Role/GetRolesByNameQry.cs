using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Repositories.Domain.Administration.User;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Administration.Role;

public record GetRolesByNameQry(string SearchTerm) : IRequest<IEnumerable<RoleDTO>>;

public class GetRolesByNameQryHandler(
   IRoleReadRepo roleReadRepo)
    : IRequestHandler<GetRolesByNameQry, IEnumerable<RoleDTO>>
{

    public async Task<IEnumerable<RoleDTO>> Handle(GetRolesByNameQry request, CancellationToken cancellationToken)
    {
        DataGridIntent intent = new()
        {
            Take = 9999,
            Filters =
            [
                new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(RoleDTO.Name),
                    Value = request.SearchTerm,
                    ComparisonOperator = ComparisonOperatorEnum.Contains,
                    FilterValueType = FilterValueTypeEnum.String
                },
                new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(RoleDTO.Active),
                    Value = 1,
                    ComparisonOperator = ComparisonOperatorEnum.Equals,
                    FilterValueType = FilterValueTypeEnum.Boolean
                }
            ]
        };

        var dto = await roleReadRepo.GetRoleTableDetails(intent);

        return dto.data;
    }
}
