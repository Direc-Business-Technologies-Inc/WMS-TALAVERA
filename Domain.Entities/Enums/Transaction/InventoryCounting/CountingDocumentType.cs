using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.InventoryCounting;

public enum CountingDocumentType
{
    [Description("Document")]
    Document,

    [Description("Sheet")]
    Sheet,
}
