using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IItemsHandler
{
    Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<ItemsVM> Data, int Count)> GetItemsAtLocationDataGridAsync(DataGridIntent intent, int locationId, int? subsidiaryId = null, CancellationToken ct = default);
    Task<ItemsVM> GetItemsAsync(string id, CancellationToken ct = default);
    Task<(IEnumerable<ItemUnitVM> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent, CancellationToken ct = default);
}
