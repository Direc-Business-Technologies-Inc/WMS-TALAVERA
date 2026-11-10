using Application.DataTransferObjects.Transactions.SupplierReturn;

namespace Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;

public enum BulkFulfillmentEventReceipt { Accepted, Duplicate, Conflict, Invalid, Unavailable }

public interface IBulkFulfillmentTrackingRepository
{
    Task<Guid> BeginAsync(Guid userId, IReadOnlyList<int> returnIds, CancellationToken ct);
    Task CompleteSubmissionAsync(Guid requestId, BulkFulfillmentResultDTO result, CancellationToken ct);
    Task<BulkFulfillmentEventReceipt> ReceiveAsync(BulkFulfillmentEventDTO data, CancellationToken ct);
    Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> GetUnreadAsync(Guid userId, CancellationToken ct);
    Task DismissAsync(Guid userId, Guid requestId, long sequence, CancellationToken ct);
}
