using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class LocationHandler(ISender sender) : ILocationHandler
{
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsAsync(DataGridIntent intent)
    {
        GetLocationsQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }

    public async Task<(IEnumerable<LocationBinVM> Data, int Count)> GetLocationBinsAsync(int locationId, DataGridIntent intent)
    {
        GetLocationBinsQry query = new(locationId, intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<LocationBinVM>>(), count);
    }
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId)
    {
        GetLocationsBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }
}
