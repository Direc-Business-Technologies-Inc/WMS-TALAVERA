namespace Integration.SAP.Entities.Transactional.Receiving;

public class PurchaseDeliveryNoteLineSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int LineNum { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string? ISBN { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpenQty { get; set; }
    public string UoMCode { get; set; }
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; }
    public string InputType { get; set; }
}
