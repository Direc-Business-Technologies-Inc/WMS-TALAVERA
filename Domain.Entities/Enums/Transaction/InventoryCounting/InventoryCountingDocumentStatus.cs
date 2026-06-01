using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.InventoryCounting;

public enum InventoryCountingDocumentStatus
{
    [Description("Open")]
    Open,

    [Description("Saved")]
    Saved,

    [Description("Posted")]
    Posted,

    [Description("Recount")]
    Recount,
}
