using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.System;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.System;

public record GetNavigationRoutesQry : IRequest<IEnumerable<NavigationRouteDTO>>;

public class GetNavigationRoutesQryHandler(
    IAppReadRepository appReadRepository)
    : IRequestHandler<GetNavigationRoutesQry, IEnumerable<NavigationRouteDTO>>
{
    public async Task<IEnumerable<NavigationRouteDTO>> Handle(GetNavigationRoutesQry request, CancellationToken cancellationToken)
    {
        var response = await appReadRepository.GetAllAsync<NavigationRouteDEM>();

        Dictionary<Guid, NavigationRouteDEM> allRoutes = response.ToDictionary(
            r => r.Id,
            r => r);

        HashSet<NavigationRouteDEM> result = [];
        HashSet<Guid> visited = [];

        foreach (var route in response)
        {
            if (route == null)
                continue;

            if (visited.Add(route.Id)) 
                result.Add(route);

            var currentRoute = route;
            while(currentRoute.ParentId.HasValue && allRoutes.TryGetValue(currentRoute.ParentId.Value, out var parent))
            {
                if (!visited.Add(parent.Id))
                    break;

                result.Add(parent);
                currentRoute = parent;
            }
        }

        return result.Adapt<List<NavigationRouteDTO>>();
    }
}
