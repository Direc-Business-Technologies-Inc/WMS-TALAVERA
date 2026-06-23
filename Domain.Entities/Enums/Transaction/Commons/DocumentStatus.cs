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

    [Description("For Approval")]
    A,
    [Description("Pending Receipt")]
    B,
    [Description("Partially Received")]
    D,
    [Description("Pending Billing")]
    E,
    [Description("Fully Billed")]
    F,
    [Description("Closed")]
    G,
    [Description("Rejected")]
    H
}
