using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;

public interface IInventoryTransferRequestIntegration
{
    Task<(IEnumerable<InventoryTransferRequestDataGridDTO> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent);
    Task<IEnumerable<InventoryTransferRequestLineDTO>> GetInventoryTransferRequestLinesAsync(string Ref);
    Task<InventoryTransferRequestDTO?> GetInventoryTransferRequestAsync(string Ref);
    Task<bool> CreateInventoryTransferRequest(InventoryTransferRequestDTO data);
    Task<(IEnumerable<InventoryTransferRequestStatusDTO>, int)> GetStatusTypesAsync(DataGridIntent intent);

}
