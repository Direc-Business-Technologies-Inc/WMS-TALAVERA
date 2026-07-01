using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.Location;

public record GetParentLocationQry(int Id) : IRequest<LocationDTO?>;  

public class GetParentLocationQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetParentLocationQry, LocationDTO?>
{
    public async Task<LocationDTO?> Handle(GetParentLocationQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetParentLocation(request.Id);
    }
}