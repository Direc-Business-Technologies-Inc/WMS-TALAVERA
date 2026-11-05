using Application.DataTransferObjects.Others;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ILocationIntegration
{
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<LocationDTO?> GetParentLocation(int locationId, CancellationToken ct = default);
    Task<LocationDTO?> GetLocation(int locationId, CancellationToken ct = default);
    Task<(IEnumerable<LocationBinDTO> data, int count)> GetLocationBinsAsync(int locationId, DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default);
    Task<(IEnumerable<LocationDTO> data, int count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location, CancellationToken ct = default);
    Task<(IEnumerable<LocationDTO> data, int count)> GetCurrentUserAllowedLocations(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<LocationDTO> data, int count)> GetUserAllowedLocations(DataGridIntent intent, int employeeId, CancellationToken ct = default);
}
