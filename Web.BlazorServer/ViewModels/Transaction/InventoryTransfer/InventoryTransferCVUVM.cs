using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

public class InventoryTransferCVUVM
{
    public int DocNum { get; set; }
    public int DocEntry { get; set; }
    public DateTime DocDate { get; set; }
    public WarehouseVM FromWarehouse { get; set; }
    public WarehouseVM ToWarehouse { get; set; }
    public TransferTypeVM TransferType { get; set; }
    public string Remarks { get; set; }
    public string PreparedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string NotedBy { get; set; }
    public string SchoolYear { get; set; }
    public List<InventoryTransferCVULineVM> Lines { get; set; } = [];
}
