namespace Integration.SAP.Entities.Transactional.InventoryTransfer;

public class InventoryTransferDataGridSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public string FromWhsName { get; set; }
    public string ToWhsName { get; set; }
    public string TransferType { get; set; }
    public string Remarks { get; set; }
    public string PreparedBy { get; set; }

}
