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


public record GetLocationSubLocationsQry(DataGridIntent intent, int location) : IRequest<(IEnumerable<LocationDTO> Data, int Count)>;

public class GetLocationSubLocationsQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetLocationSubLocationsQry, (IEnumerable<LocationDTO> Data, int Count)>
{
    public async Task<(IEnumerable<LocationDTO> Data, int Count)> Handle(GetLocationSubLocationsQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetSublocationsOfLocationAsync(request.intent, request.location);
    }
}