using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class ItemsHandler(ISender sender) : IItemsHandler
{
    public Task<ItemsVM> GetItemsAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent)
    {
        var query = new GetItemsQry(intent);
        (var data, var count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<ItemsVM>>(), count);
    }
}