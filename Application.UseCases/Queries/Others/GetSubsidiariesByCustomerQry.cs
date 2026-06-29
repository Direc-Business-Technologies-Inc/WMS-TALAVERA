using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others;

public record GetSubsidiariesByCustomerQry(DataGridIntent Intent, int customerId) : IRequest<(IEnumerable<SubsidiaryDTO>, int)>;

public class GetSubsidiariesByCustomerQryHandler(
        ISubsidiaryIntegration integration
    ) : IRequestHandler<GetSubsidiariesByCustomerQry, (IEnumerable<SubsidiaryDTO>, int)>
{
    public async Task<(IEnumerable<SubsidiaryDTO>, int)> Handle(GetSubsidiariesByCustomerQry request, CancellationToken cancellationToken)
    {
        return await integration.GetSubsidiariesByCustomerAsync(request.Intent, request.customerId);
    }
}
