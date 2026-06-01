using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.Administration.User;
using Domain.Entities.System;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.System;

public record GetAllowedRoutesQry(Guid UserId) : IRequest<IEnumerable<NavigationRouteDTO>>;

public class GetAllowedRoutesQryHandler(
    IAppReadRepository appReadRepository,
    IUserReadRepo userReadRepo)
    : IRequestHandler<GetAllowedRoutesQry, IEnumerable<NavigationRouteDTO>>
{
    public async Task<IEnumerable<NavigationRouteDTO>> Handle(GetAllowedRoutesQry request, CancellationToken cancellationToken)
    {
        var response = await appReadRepository.GetAllAsync<NavigationRouteDEM>();
        var allowedRoutesResponse = await userReadRepo.GetUserRoutes(request.UserId);

        response.RemoveAll(r => !allowedRoutesResponse.Any(ar => ar.Id == r.Id) && r.Protected);

        Dictionary<Guid, NavigationRouteDEM> allRoutes = response.ToDictionary(
            r => r.Id,
            r => r);

        HashSet<NavigationRouteDTO> result = [];
        HashSet<Guid> visited = [];

        foreach (var route in allowedRoutesResponse)
        {
            if (route == null)
                continue;

            if (visited.Add(route.Id))
                result.Add(route);

            var currentRoute = route;
            while (currentRoute.ParentId.HasValue && allRoutes.TryGetValue(currentRoute.ParentId.Value, out var parent))
            {
                if (!visited.Add(parent.Id))
                    break;

                result.Add(parent.Adapt<NavigationRouteDTO>());
                currentRoute = parent.Adapt<NavigationRouteDTO>();
            }
        }

        return result.Adapt<List<NavigationRouteDTO>>();
    }
}

