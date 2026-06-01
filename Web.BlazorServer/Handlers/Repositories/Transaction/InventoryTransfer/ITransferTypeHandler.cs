using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;

public interface ITransferTypeHandler
{
    Task<IEnumerable<TransferTypeVM>> GetTransferTypesAsync();
}
