using Application.DataTransferObjects.Others.SAP;
using Shared.Entities;


namespace Application.UseCases.Repositories.Integration.Others;

public interface IItemMasterDataIntegration
{
    Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetMerchandiseItems(DataGridIntent intent);
    Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetWarehouseItems(DataGridIntent intent, string whsCode);
    Task<(IEnumerable<ItemSelectionSAPDTO> Data, int Count)> GetItemWarehouseLevel(DataGridIntent intent, string whsCode, List<string> itemCodes);
    Task<IEnumerable<InventoryCountingItemSAPDTO>> GetWarehouseItemsForCounting(string whsCode);
}
