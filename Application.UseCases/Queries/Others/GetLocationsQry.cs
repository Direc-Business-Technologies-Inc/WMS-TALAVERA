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

public record GetLocationsQry(DataGridIntent intent) : IRequest<(IEnumerable<LocationDTO> Data, int Count)>;

public class GetLocationsQryHandler(ILocationIntegration locationIntegration) : IRequestHandler<GetLocationsQry, (IEnumerable<LocationDTO> Data, int Count)>
{
    public async Task<(IEnumerable<LocationDTO> Data, int Count)> Handle(GetLocationsQry request, CancellationToken cancellationToken)
    {
        return await locationIntegration.GetLocationsAsync(request.intent);
    }
}
