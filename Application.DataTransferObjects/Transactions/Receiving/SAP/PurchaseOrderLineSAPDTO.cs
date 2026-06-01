namespace Integration.SAP.Entities.Transactional.Receiving;

public class PurchaseOrderLineSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int LineNum { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string? ISBN { get; set; }
    public decimal Price { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public string UoMCode { get; set; }
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public string VatGroup { get; set; }
}
