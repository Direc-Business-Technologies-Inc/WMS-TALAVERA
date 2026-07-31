using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.Location;


public record GetCurrentUserLocationsQry(DataGridIntent Intent) : IRequest<(IEnumerable<LocationDTO>, int count)>;

public class GetCurrentUserLocationQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetCurrentUserLocationsQry, (IEnumerable<LocationDTO>, int count)>
{
    public async Task<(IEnumerable<LocationDTO>, int count)> Handle(GetCurrentUserLocationsQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetCurrentUserAllowedLocations(request.Intent);
    }
}