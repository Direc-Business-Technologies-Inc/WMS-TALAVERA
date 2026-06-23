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

public record GetLocationBinsQry(int locationId, DataGridIntent Intent) : IRequest<(IEnumerable<LocationBinDTO> data, int count)>;

public class GetLocationBinsQryHandler(ILocationIntegration integration)
    : IRequestHandler<GetLocationBinsQry, (IEnumerable<LocationBinDTO> data, int count)>
{
    public async Task<(IEnumerable<LocationBinDTO> data, int count)> Handle(GetLocationBinsQry request, CancellationToken cancellationToken)
    {
        return await integration.GetLocationBinsAsync(request.locationId, request.Intent);
    }
}