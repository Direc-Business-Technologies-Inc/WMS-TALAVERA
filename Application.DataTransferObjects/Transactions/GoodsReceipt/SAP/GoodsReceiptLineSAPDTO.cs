namespace Application.DataTransferObjects.Transactions.GoodsReceipt;

public class GoodsReceiptLineSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public int LineNum { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string UoMCode { get; set; }
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; }
}
