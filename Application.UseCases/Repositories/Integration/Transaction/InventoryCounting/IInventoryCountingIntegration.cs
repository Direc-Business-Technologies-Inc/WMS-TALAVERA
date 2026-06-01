using Application.DataTransferObjects.Transactions.InventoryCounting;

namespace Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;

public interface IInventoryCountingIntegration
{
    Task<bool> PostInventoryCountings(InventoryCountingDocumentDTO data);
}
