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

public record GetLocationsBySubsidiaryQry(DataGridIntent intent, int subsidiaryID) : IRequest<(IEnumerable<LocationDTO> Data, int Count)>;

public class GetLocationsBySubsidiaryQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetLocationsBySubsidiaryQry, (IEnumerable<LocationDTO> Data, int Count)>
{
    public async Task<(IEnumerable<LocationDTO> Data, int Count)> Handle(GetLocationsBySubsidiaryQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetLocationsBySubsidiaryAsync(request.intent, request.subsidiaryID);
    }
}