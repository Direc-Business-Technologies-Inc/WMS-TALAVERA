using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.Commons;

public enum DocumentStatus
{
    [Description("Open")]
    Open,

    [Description("Closed")]
    Closed,

    [Description("Cancelled")]
    Cancelled,
}
