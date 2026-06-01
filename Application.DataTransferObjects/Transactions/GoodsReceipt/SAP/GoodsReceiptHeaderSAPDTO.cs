namespace Application.DataTransferObjects.Transactions.GoodsReceipt;

public class GoodsReceiptHeaderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public int Time { get; set; }
    public string PreparedBy { get; set; }
    public string TransTypeCode { get; set; }
    public string TransTypeName { get; set; }
    public string AcctCode { get; set; }
    public string AcctName { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string WarNo { get; set; }
    public string PurNo { get; set; }
    public string DocRemarks { get; set; }
    public string Designation { get; set; }
    public string ReceivedBy { get; set; }
    public string ApprovedBy { get; set; }
    public string NotedBy { get; set; }

}
