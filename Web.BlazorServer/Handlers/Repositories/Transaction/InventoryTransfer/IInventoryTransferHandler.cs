using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer
{
    public interface IInventoryTransferHandler
    {
        Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferDataGridAsync(DataGridIntent intent);
        Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetPostedInventoryTransferDataGridAsync(DataGridIntent intent);
        Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetPendingInventoryTransferDataGridAsync(DataGridIntent intent);
        Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetRejectedInventoryTransferDataGridAsync(DataGridIntent intent);
        Task<InventoryTransferRequestVM> GetInventoryTransferRequestAsync(int id);
        Task<InventoryTransferCVUVM> GetPostedInventoryTransferRequestAsync(int id);
        Task<InventoryTransferCVUVM> GetPendingInventoryTransferRequestAsync(int id);
        Task<InventoryTransferCVUVM> GetRejectedInventoryTransferRequestAsync(int id);
        Task<int> PostInventoryTransferRequestAsync(InventoryTransferRequestVM data);

    }
}
