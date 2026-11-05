using Application.UseCases.Queries.Others;
using Application.UseCases.Queries.Others.Location;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class LocationHandler(ISender sender) : ILocationHandler
{
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetLocationsQry query = new(intent);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetCurrentUserLocationsAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetCurrentUserLocationsQry query = new(intent);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location, CancellationToken ct = default)
    {
        GetLocationSubLocationsQry query = new(intent, location);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }

    public async Task<(IEnumerable<LocationBinVM> Data, int Count)> GetLocationBinsAsync(int locationId, DataGridIntent intent, CancellationToken ct = default)
    {
        GetLocationBinsQry query = new(locationId, intent);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<LocationBinVM>>(), count);
    }
    public async Task<(IEnumerable<LocationVM> Data, int Count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default)
    {
        GetLocationsBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<LocationVM>>(), count);
    }
    public async Task<(IEnumerable<InventoryBalanceVM> Data, int Count)> GetLocationInventoryBalanceAsync(int locationId, DataGridIntent intent, CancellationToken ct = default)
    {
        GetLocationInventoryBalanceQry query = new(intent, locationId);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<InventoryBalanceVM>>(), count);
    }

    public async Task<LocationVM?> GetParentLocation(LocationVM location, CancellationToken ct = default)
    {
        GetParentLocationQry query = new(location.Id);
        var data = await sender.Send(query, ct);
        return data?.Adapt<LocationVM>();
    }

    public async Task<LocationVM?> GetLocation(int locationId, CancellationToken ct = default)
    {
        GetLocationQry query = new(locationId);
        var data = await sender.Send(query, ct);
        return data?.Adapt<LocationVM>();
    }
}
