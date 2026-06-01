using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
public interface IInventoryTransferIntegration
{
    #region Inventory Transfer Requests
    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetInventoryTransferRequestListAsync(DataGridIntent intent);
    public Task<InventoryTransferRequestHeaderSAPDTO?> GetInventoryTransferRequestHeaderAsync(int docEntry);
    public Task<IEnumerable<InventoryTransferRequestLineSAPDTO>> GetInventoryTransferRequestLinesAsync(int docEntry);

    #endregion
    #region Posted Requests
    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPostedInventoryTransferRequestListAsync(DataGridIntent intent);
    public Task<InventoryTransferHeaderSAPDTO?> GetPostedInventoryTransferRequestHeaderAsync(int docEntry);
    public Task<IEnumerable<InventoryTransferLineSAPDTO>> GetPostedInventoryTransferRequestLinesAsync(int docEntry);

    #endregion
    #region Pending Requests
    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPendingInventoryTransferRequestListAsync(DataGridIntent intent);
    #endregion
    #region Rejected Requests
    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetRejectedInventoryTransferRequestListAsync(DataGridIntent intent);
    #endregion
    public Task<InventoryTransferHeaderSAPDTO?> GetInventoryTransferRequestDraftHeaderAsync(int docEntry, string status);
    public Task<IEnumerable<InventoryTransferLineSAPDTO>> GetInventoryTransferRequestDraftLinesAsync(int docEntry);
    public Task<int> PostInventoryTransferRequest(InventoryTransferRequestDTO dto);
}

