namespace Application.DataTransferObjects.Transactions.GoodsReturn.SAP;

public class GoodsReturnHeaderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int GRRDocEntry { get; set; }
    public int GRRDocNum { get; set; }
    public string ReturnType { get; set; }
    public int GRPODocEntry { get; set; }
    public int GRPODocNum { get; set; }
    public int PODocEntry { get; set; }
    public int PODocNum { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public string Remarks { get; set; }
    public string? SchoolYear { get; set; }
    public string? DRNo { get; set; }
    public string? SINo { get; set; }
    public string? DeliveredBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? DocRemarks { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? CheckedBy { get; set; }

}
