using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ILocationHandler
{
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsAsync(DataGridIntent intent);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetCurrentUserLocationsAsync(DataGridIntent intent);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId);
    Task<(IEnumerable<LocationBinVM> Data, int Count)> GetLocationBinsAsync(int locationId, DataGridIntent intent);
    Task<(IEnumerable<InventoryBalanceVM> Data, int Count)> GetLocationInventoryBalanceAsync(int locationId, DataGridIntent intent);
    Task<LocationVM?> GetParentLocation(LocationVM location);
    Task<LocationVM?> GetLocation(int locationId);
}
