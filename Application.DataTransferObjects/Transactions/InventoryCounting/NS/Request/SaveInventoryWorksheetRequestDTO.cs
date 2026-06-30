using Shared.Libraries.ViewModel.InventoryCounting;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS.Request;

public class SaveInventoryWorksheetRequestDTO
{
    public List<InventoryWorksheetLineDTO> InventoryCountItems { get; set; }
    public int Location { get; set; }
}
