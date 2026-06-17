using Application.DataTransferObjects.Others;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ILocationIntegration
{
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent);
    Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId);
}
