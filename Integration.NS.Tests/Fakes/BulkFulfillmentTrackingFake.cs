using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;

namespace Integration.NS.Tests.Fakes;

public class BulkFulfillmentTrackingFake : IBulkFulfillmentTrackingRepository
{
    public Task<Guid> BeginAsync(Guid userId, IReadOnlyList<int> returnIds, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
    public Task CompleteSubmissionAsync(Guid requestId, BulkFulfillmentResultDTO result, CancellationToken ct) => Task.CompletedTask;
    public Task<BulkFulfillmentEventReceipt> ReceiveAsync(BulkFulfillmentEventDTO data, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> GetUnreadAsync(Guid userId, CancellationToken ct) => throw new NotSupportedException();
    public Task DismissAsync(Guid userId, Guid requestId, long sequence, CancellationToken ct) => throw new NotSupportedException();
}
