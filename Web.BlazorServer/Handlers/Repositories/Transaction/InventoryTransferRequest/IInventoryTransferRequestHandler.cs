using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;

public interface IInventoryTransferRequestHandler
{

    Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent);
    Task<InventoryTransferRequestVM?> GetInventoryTransferRequestAsync(string Ref);
}
