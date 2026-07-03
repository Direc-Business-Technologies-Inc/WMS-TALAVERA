using Application.UseCases.Queries.System;
using Dapper;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Implementations.System;

public class NavigationRouteHandler(
    ISender Sender)
    : INavigationRouteHandler
{
    public async Task<IEnumerable<NavigationRouteVM>> GetAllowedRoutesAsync(Guid userId)
    {
        return NavRoutesRepository.Instance.Roots;
    }

    public async Task<IEnumerable<NavigationRouteVM>> GetAllRoutesAsync()
    {
        return NavRoutesRepository.Instance.Roots;
    }

    public async Task<IEnumerable<NavigationRouteVM>> GetModuleNavigationRoutesAsync(string moduleCode)
    {
        return NavRoutesRepository.Instance.GetPath(moduleCode);
    }
}
