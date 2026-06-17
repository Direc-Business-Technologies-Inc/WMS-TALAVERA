using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IItemsHandler
{
    Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsAtLocationDataGridAsync(DataGridIntent intent, int locationId);
    Task<ItemsVM> GetItemsAsync(string id);
}
