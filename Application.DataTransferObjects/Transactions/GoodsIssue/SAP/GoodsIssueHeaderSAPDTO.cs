using Application.DataTransferObjects.Others;

namespace Application.DataTransferObjects.Transactions.GoodsIssue;

public class GoodsIssueHeaderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public string PreparedBy { get; set; }
    public string TransTypeCode { get; set; }
    public string TransTypeName { get; set; }
    public string AcctCode { get; set; }
    public string AcctName { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string SchoolYear { get; set; }
    public string SrfNo { get; set; }
    public string DocRemarks { get; set; }
    public string Designation { get; set; }
    public string ReceivedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string NotedBy { get; set; }
}
