using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.InventoryCounting;

public enum InventoryCountingSheetStatus
{
    [Description("Synced")]
    Synced,

    [Description("Async")]
    Async,

    [Description("Ignore")]
    Ignore,
}
