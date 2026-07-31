using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IInventoryHandler
{
    Task<(IEnumerable<InventoryStatusVM>, int)> GetInventoryStatusAsync(DataGridIntent intent);
    Task<IEnumerable<InventoryDetailVM>> GetInventoryDetails(int documentId, int lineId);
    Task<(IEnumerable<InventoryBalanceVM>, int)> GetInventoryBalanceAsync(
        DataGridIntent intent,
        int? locationId = null,
        int? itemId = null,
        int? binId = null,
        int? statusId = null
    );
}
