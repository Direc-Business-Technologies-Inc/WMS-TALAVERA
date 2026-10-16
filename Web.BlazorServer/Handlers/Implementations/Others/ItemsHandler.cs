using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class ItemsHandler(ISender sender) : IItemsHandler
{
    public Task<ItemsVM> GetItemsAsync(string id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsAtLocationDataGridAsync(DataGridIntent intent, int locationId, int? subsidiaryId = null, CancellationToken ct = default)
    {
        var query = new GetItemsByLocationQry(intent, locationId, subsidiaryId);
        (var data, var count) = await sender.Send(query, ct);

        return (data.Adapt<IEnumerable<ItemsVM>>(), count);
    }

    public async Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var query = new GetItemsQry(intent);
        (var data, var count) = await sender.Send(query, ct);

        return (data.Adapt<IEnumerable<ItemsVM>>(), count);
    }

    public async Task<(IEnumerable<ItemUnitVM> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent, CancellationToken ct = default)
    {
        var query = new GetItemUnitsQry(itemId, intent);
        (var data, var count) = await sender.Send(query, ct);

        return (data.Adapt<IEnumerable<ItemUnitVM>>(), count);
    }
}