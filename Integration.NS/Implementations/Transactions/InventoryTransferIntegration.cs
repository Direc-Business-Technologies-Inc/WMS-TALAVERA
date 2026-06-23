using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class InventoryTransferIntegration : IInventoryTransferIntegration
{
    public Task<InventoryTransferHeaderSAPDTO?> GetInventoryTransferRequestDraftHeaderAsync(int docEntry, string status)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryTransferLineSAPDTO>> GetInventoryTransferRequestDraftLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<InventoryTransferRequestHeaderSAPDTO?> GetInventoryTransferRequestHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryTransferRequestLineSAPDTO>> GetInventoryTransferRequestLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPendingInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<InventoryTransferHeaderSAPDTO?> GetPostedInventoryTransferRequestHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryTransferLineSAPDTO>> GetPostedInventoryTransferRequestLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPostedInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetRejectedInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<int> PostInventoryTransferRequest(InventoryTransferRequestDTO dto)
    {
        throw new NotImplementedException();
    }
}
