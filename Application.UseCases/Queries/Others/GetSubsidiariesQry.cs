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

public record GetSubsidiariesQry(DataGridIntent Intent) : IRequest<(IEnumerable<SubsidiaryDTO> Data, int Count)>;
public class GetSubsidiariesQryHandler(ISubsidiaryIntegration subsidiaryIntegration)
    : IRequestHandler<GetSubsidiariesQry, (IEnumerable<SubsidiaryDTO> Data, int Count)>
{
    public async Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> Handle(GetSubsidiariesQry request, CancellationToken cancellationToken)
    {
        return await subsidiaryIntegration.GetSubsidiariesAsync(request.Intent);
    }
}
