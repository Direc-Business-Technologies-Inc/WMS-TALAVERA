using System.ComponentModel;

namespace Domain.Enums.Transaction.Commons;

public enum ApprovalStatus
{
    [Description("None")]
    None = 0,

    [Description("For approval")]
    ForApproval = 1,

    [Description("Ongoing approval")]
    OngoingApproval = 2,

    [Description("For review")]
    ForReview = 3,

    [Description("Rejected")]
    Rejected = 4,

    [Description("Approved")]
    Approved = 5
}
