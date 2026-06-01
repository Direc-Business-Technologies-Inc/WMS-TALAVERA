using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Repositories.System;

public interface INavigationRouteHandler
{
    Task<IEnumerable<NavigationRouteVM>> GetAllRoutesAsync(); 
    Task<IEnumerable<NavigationRouteVM>> GetAllowedRoutesAsync(Guid userId); 
    Task<IEnumerable<NavigationRouteVM>> GetModuleNavigationRoutesAsync(string moduleCode); 
}
