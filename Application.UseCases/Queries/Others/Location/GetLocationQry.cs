using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.Location;

public record GetLocationQry(int Id) : IRequest<LocationDTO?>;

public class GetLocationQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetLocationQry, LocationDTO?>
{
    public async Task<LocationDTO?> Handle(GetLocationQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetLocation(request.Id);
    }
}