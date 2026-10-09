using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ILocationHandler
{
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetCurrentUserLocationsAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location, CancellationToken ct = default);
    Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default);
    Task<(IEnumerable<LocationBinVM> Data, int Count)> GetLocationBinsAsync(int locationId, DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<InventoryBalanceVM> Data, int Count)> GetLocationInventoryBalanceAsync(int locationId, DataGridIntent intent, CancellationToken ct = default);
    Task<LocationVM?> GetParentLocation(LocationVM location, CancellationToken ct = default);
    Task<LocationVM?> GetLocation(int locationId, CancellationToken ct = default);
}
