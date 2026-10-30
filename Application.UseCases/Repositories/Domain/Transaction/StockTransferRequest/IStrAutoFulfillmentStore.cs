using Application.DataTransferObjects.Transactions.StockTransferRequest;

namespace Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;

/// <summary>Shared exclusion and durable automatic-post outcomes. All methods commit before returning.</summary>
public interface IStrAutoFulfillmentStore
{
    Task<Guid?> TryAcquireAsync(int netsuiteOrderId, CancellationToken ct);
    Task<bool> RenewAsync(int netsuiteOrderId, Guid owner, CancellationToken ct);
    Task ReleaseAsync(int netsuiteOrderId, Guid owner, CancellationToken ct);
    Task MarkManualPostingAsync(int netsuiteOrderId, Guid owner, CancellationToken ct);
    Task CompleteManualAsync(int netsuiteOrderId, Guid owner, CancellationToken ct);
    Task<bool> TryStartAutoAsync(int netsuiteOrderId, Guid owner, CancellationToken ct);
    Task MarkManualUncertainAsync(int netsuiteOrderId, Guid owner, string reason, CancellationToken ct);
    Task RecordAutoOutcomeAsync(int netsuiteOrderId, Guid owner, string state, string? reason, string? fulfillmentIds, CancellationToken ct);
    Task RecordDecisionAsync(int netsuiteOrderId, string state, string reason, CancellationToken ct);
    Task<DateTime> GetOrCreateRouteCutoffAsync(string routeKey, CancellationToken ct);
    Task<IReadOnlyList<StrAutoFulfillmentOutcomeDTO>> RecentOutcomesAsync(int take, CancellationToken ct);
}
