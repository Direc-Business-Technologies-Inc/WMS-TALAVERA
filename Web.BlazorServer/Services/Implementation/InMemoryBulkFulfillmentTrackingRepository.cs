using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;

namespace Web.BlazorServer.Services.Implementation;

/// <summary>Process-local routing and dismissal state. No database or restart durability.</summary>
public class InMemoryBulkFulfillmentTrackingRepository : IBulkFulfillmentTrackingRepository
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, Request> requests = [];
    private readonly Dictionary<Guid, (BulkFulfillmentEventDTO Event, DateTime Received)> events = [];
    private const int Capacity = 10000;

    private sealed class Request(Guid userId)
    {
        public Guid UserId { get; } = userId;
        public int? BulkFulfillmentId { get; set; }
        public long DismissedSequence { get; set; }
    }

    public Task<Guid> BeginAsync(Guid userId, IReadOnlyList<int> returnIds, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (requests.Count >= Capacity) throw new InvalidOperationException("Bulk notification tracking capacity reached. Try again later.");
            var id = Guid.NewGuid();
            requests.Add(id, new(userId));
            return Task.FromResult(id);
        }
    }

    public Task CompleteSubmissionAsync(Guid requestId, BulkFulfillmentResultDTO result, CancellationToken ct)
    {
        lock (gate)
        {
            if (requests.TryGetValue(requestId, out var request))
            {
                if (result.Outcome == BulkFulfillmentOutcome.Rejected) requests.Remove(requestId);
                else request.BulkFulfillmentId = result.ReferenceId;
            }
        }
        return Task.CompletedTask;
    }

    public Task<BulkFulfillmentEventReceipt> ReceiveAsync(BulkFulfillmentEventDTO data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (events.TryGetValue(data.EventId, out var existing))
                return Task.FromResult(existing.Event == data ? BulkFulfillmentEventReceipt.Duplicate : BulkFulfillmentEventReceipt.Conflict);
            if (events.Values.Any(row => row.Event.BulkFulfillmentId == data.BulkFulfillmentId && row.Event.Sequence == data.Sequence))
                return Task.FromResult(BulkFulfillmentEventReceipt.Conflict);
            if (events.Count >= Capacity) return Task.FromResult(BulkFulfillmentEventReceipt.Unavailable);
            events.Add(data.EventId, (data, DateTime.UtcNow));
            return Task.FromResult(BulkFulfillmentEventReceipt.Accepted);
        }
    }

    public Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> GetUnreadAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            var latest = events.Values.GroupBy(row => row.Event.BulkFulfillmentId)
                .ToDictionary(group => group.Key, group => group.MaxBy(row => row.Event.Sequence).Event);
            IReadOnlyList<BulkFulfillmentNotificationDTO> result = requests
                .Where(pair => pair.Value.UserId == userId && pair.Value.BulkFulfillmentId.HasValue)
                .Where(pair => latest.TryGetValue(pair.Value.BulkFulfillmentId!.Value, out var evt) && evt.Sequence > pair.Value.DismissedSequence)
                .Select(pair => { var evt = latest[pair.Value.BulkFulfillmentId!.Value]; return new BulkFulfillmentNotificationDTO(pair.Key, evt.BulkFulfillmentId, evt.Sequence, evt.Status, evt.Message); })
                .Take(50).ToArray();
            return Task.FromResult(result);
        }
    }

    public Task DismissAsync(Guid userId, Guid requestId, long sequence, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (requests.TryGetValue(requestId, out var request) && request.UserId == userId
                && events.Values.Any(row => row.Event.BulkFulfillmentId == request.BulkFulfillmentId && row.Event.Sequence == sequence))
                request.DismissedSequence = Math.Max(request.DismissedSequence, sequence);
        }
        return Task.CompletedTask;
    }
}
