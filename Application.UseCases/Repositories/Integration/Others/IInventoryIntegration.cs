using Application.DataTransferObjects.Others.Inventory;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IInventoryIntegration
{
    Task<(IEnumerable<InventoryBalanceDTO>, int)> GetInventoryBalance(DataGridIntent intent);
    Task<(IEnumerable<InventoryStatusDTO>, int)> GetInventoryStatus(DataGridIntent intent);
}
