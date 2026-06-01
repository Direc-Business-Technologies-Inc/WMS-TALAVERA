
namespace Integration.SAP.Entities.Transactional.InventoryTransfer;
public class InventoryTransferHeaderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public string Status { get; set; }
    public string FrmWhsCode { get; set; }
    public string ToWhsCode { get; set; }
    public string FrmWhsName { get; set; }
    public string ToWhsName { get; set; }
    public string TransferTypeName { get; set; }
    public string TransferTypeCode { get; set; }
    public string SchoolYear { get; set; }
    public string Remarks { get; set; }
    public string PreparedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string NotedBy { get; set; }
}
