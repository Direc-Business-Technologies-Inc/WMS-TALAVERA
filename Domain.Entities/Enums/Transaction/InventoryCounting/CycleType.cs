using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.InventoryCounting;

public enum CycleType
{
    [Description("Daily")]
    Daily,

    [Description("Weekly")]
    Weekly,

    [Description("Monthly")]
    Monthly,

    [Description("Quarterly")]
    Quarterly,
}
