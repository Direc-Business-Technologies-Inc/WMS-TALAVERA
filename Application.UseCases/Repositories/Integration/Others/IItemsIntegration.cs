using Application.DataTransferObjects.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IItemsIntegration
{
    Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsByLocationDataGridAsync(DataGridIntent intent, int location, int? subsidiary = null, CancellationToken ct = default);
    Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(ItemsDTO itemId, DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent, CancellationToken ct = default);
    Task<ItemsDTO?> GetItem(string  id, CancellationToken ct = default);
}
