using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others
{
    public record GetSubsidiaryQry(int id) : IRequest<SubsidiaryDTO?>;

    public class GetSubsidiaryQryHandler(
        ISubsidiaryIntegration integration) : IRequestHandler<GetSubsidiaryQry, SubsidiaryDTO?>
    {
        public Task<SubsidiaryDTO?> Handle(GetSubsidiaryQry request, CancellationToken cancellationToken)
        {
            return integration.GetSubsidiaryAsync(request.id);
        }
    }
}
