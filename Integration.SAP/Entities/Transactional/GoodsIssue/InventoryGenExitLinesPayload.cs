using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.GoodsIssue;

public class InventoryGenExitLinesPayload
{
    public string ItemCode { get; private set; }
    public string WarehouseCode { get; private set; }
    public string AccountCode { get; private set; }
    public decimal Quantity { get; private set; }

    public InventoryGenExitLinesPayload(string itemCode, string warehouseCode, string accountCode, decimal qty)
    {
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Prepared By cannot be null or empty");
        WarehouseCode = Guard.Against.NullOrEmpty(warehouseCode, nameof(WarehouseCode), "Transaction Type cannot be null or empty");
        AccountCode = Guard.Against.NullOrEmpty(accountCode, nameof(AccountCode), "Account Code cannot be null or empty");
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity), "Quantity cannot be negative or zero");
    }
}
