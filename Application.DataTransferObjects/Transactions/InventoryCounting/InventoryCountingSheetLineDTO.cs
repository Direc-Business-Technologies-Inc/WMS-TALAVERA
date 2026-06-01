namespace Application.DataTransferObjects.Transactions.InventoryCounting;

public class InventoryCountingSheetLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UoMCode { get; set; } = string.Empty;
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; } = string.Empty;
}
