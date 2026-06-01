namespace Application.DataTransferObjects.Transactions.Commons;

public class ItemDTO
{
    public int LineNum { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string UoMCode { get; set; }
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; }
    public string? ISBN { get; set; }
}
