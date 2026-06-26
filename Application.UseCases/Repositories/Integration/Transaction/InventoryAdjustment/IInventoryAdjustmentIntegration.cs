using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;

public interface IInventoryAdjustmentIntegration
{
    Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetInventoryAdjustmentsAsync(DataGridIntent intent);
    Task<InventoryAdjustmentDTO?> GetInventoryAdjustmentAsync(string id);
    Task<IEnumerable<InventoryAdjustmentLineDTO>> GetInventoryAdjustmentLinesAsync(string id);
    Task<(IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent);
    Task<bool> CreateInventoryAdjustment(InventoryAdjustmentDTO value);
}
