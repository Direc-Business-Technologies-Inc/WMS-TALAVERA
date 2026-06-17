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
    Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsByLocationDataGridAsync(DataGridIntent intent, int locationId);
    Task<ItemsDTO?> GetItem(string  id);
}
