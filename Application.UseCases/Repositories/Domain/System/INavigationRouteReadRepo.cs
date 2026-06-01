using Application.DataTransferObjects.System.Commons;
using Shared.Entities;

namespace Application.UseCases.Repositories.Domain.System;

public interface INavigationRouteReadRepo
{
    Task<IEnumerable<NavigationRouteDTO>> GetModuleNavigationRoute(string moduleCode);
}
