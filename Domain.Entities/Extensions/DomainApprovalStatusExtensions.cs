using Domain.Enums.Transaction.Commons;

namespace Domain.Extensions;

public static class DomainApprovalStatusExtensions
{
    public static bool IsIn(this ApprovalStatus status, List<ApprovalStatus> statuses)
    {
        return statuses.Contains(status);
    }
}
