using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class ItemMasterDataIntegration : IItemMasterDataIntegration
{
    public Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetItemWarehouseLevel(DataGridIntent intent, string whsCode, List<string> itemCodes)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetMerchandiseItems(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetWarehouseItems(DataGridIntent intent, string whsCode)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryCountingItemSAPDTO>> GetWarehouseItemsForCounting(string whsCode)
    {
        throw new NotImplementedException();
    }
}
