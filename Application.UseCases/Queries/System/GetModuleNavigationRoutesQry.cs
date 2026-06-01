using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Domain.System;
using MediatR;

namespace Application.UseCases.Queries.System;

public record GetModuleNavigationRoutesQry(string ModuleCode) : IRequest<IEnumerable<NavigationRouteDTO>>;

public class GetModuleNavigationRoutesQryHandler(
    INavigationRouteReadRepo navigationRouteReadRepo)
    : IRequestHandler<GetModuleNavigationRoutesQry, IEnumerable<NavigationRouteDTO>>
{
    public async Task<IEnumerable<NavigationRouteDTO>> Handle(GetModuleNavigationRoutesQry request, CancellationToken cancellationToken)
    {
        var routes =  await navigationRouteReadRepo.GetModuleNavigationRoute(request.ModuleCode);
        return routes;
    }
}
