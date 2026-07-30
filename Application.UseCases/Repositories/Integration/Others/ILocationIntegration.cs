using Application.DataTransferObjects.Others;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ILocationIntegration
{
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent);
    Task<LocationDTO?> GetParentLocation(int locationId);
    Task<LocationDTO?> GetLocation(int locationId);
    Task<(IEnumerable<LocationBinDTO> data, int count)> GetLocationBinsAsync(int locationId, DataGridIntent intent);
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId);
    Task<(IEnumerable<LocationDTO> data, int count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location);
    Task<(IEnumerable<LocationDTO> data, int count)> GetCurrentUserAllowedLocations(DataGridIntent intent);
    Task<(IEnumerable<LocationDTO> data, int count)> GetUserAllowedLocations(DataGridIntent intent, int employeeId);
}
