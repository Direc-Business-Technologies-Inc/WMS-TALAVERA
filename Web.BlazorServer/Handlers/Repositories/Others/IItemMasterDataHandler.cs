using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IItemMasterDataHandler
{
    Task<(IEnumerable<ItemVM> Data, int Count)> GetMerchandiseItemsAsync(DataGridIntent intent);
    Task<(IEnumerable<ItemVM> Data, int Count)> GetWarehouseItemsAsync(DataGridIntent intent, string whsCode);
    Task<(IEnumerable<ItemVM> Data, int Count)> GetItemsInWarehouseAsync(DataGridIntent intent, string whsCode, List<string> itemCodes);
}
