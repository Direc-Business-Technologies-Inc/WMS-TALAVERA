

using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;

namespace Application.DataTransferObjects.Transactions.InventoryTransfer;

public class InventoryTransferRequestDTO
{
    public int? DocEntry { get; set; }
    public int? DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public string Status { get; set; }
    public WarehouseDTO FromWarehouse { get; set; }
    public WarehouseDTO ToWarehouse { get; set; }
    public TransferTypeDTO TransferType { get; set; }
    public SchoolYearDTO SchoolYear { get; set; }
    public string Remarks { get; set; }
    public string PreparedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string NotedBy { get; set; }
    public List<InventoryTransferRequestLineDTO> Lines { get; set; } = [];
}

