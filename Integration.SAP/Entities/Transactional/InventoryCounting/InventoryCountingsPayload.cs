using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.InventoryCounting;

public class InventoryCountingsPayload
{
    public DateTime CountDate { get; private set; }
    public string U_PrepBy { get; private set; }
    public string Remarks { get; private set; }
    public List<InventoryCountingsLinesPayload> InventoryCountingLines { get; private set; } = [];

    public InventoryCountingsPayload(DateTime cntDate, string prepBy, string remarks, List<InventoryCountingsLinesPayload> lines)
    {
        CountDate = Guard.Against.NullOrOutOfSQLDateRange(cntDate, nameof(CountDate), "Count Date must be a valid Date");
        InventoryCountingLines = [.. Guard.Against.NullOrEmpty(lines, nameof(InventoryCountingLines), "Inventory Counting Lines cannot be null or empty")];
        U_PrepBy = prepBy;
        Remarks = remarks;
    }
}
