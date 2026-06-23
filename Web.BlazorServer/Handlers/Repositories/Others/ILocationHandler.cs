using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ILocationHandler
{
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsAsync(DataGridIntent intent);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId);
}
