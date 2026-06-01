namespace Application.DataTransferObjects.Transactions.GoodsIssue;

public class GoodsIssueDataGridDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public string PreparedBy { get; set; }
    public string TransTypeCode { get; set; }
    public string TransTypeName { get; set; }
    public string AcctCode { get; set; }
    public string AcctName { get; set; }
}
