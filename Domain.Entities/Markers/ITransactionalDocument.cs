using Domain.Enums.Transaction.Commons;

namespace Domain.Markers;

public interface ITransactionalDocument : IMarker
{
    public ApprovalStatus ApprovalStatus { get; }
}
