using Domain.Commons;

namespace Domain.Entities.Transaction.StockTransferRequest;

/// <summary>Durable WMS outcome; an uncertain NetSuite POST is never retried without reconciliation.</summary>
public sealed class StrAutoFulfillmentProcessDEM : EntityDEM
{
    private StrAutoFulfillmentProcessDEM() { }

    public int NetsuiteOrderId { get; private set; }
    public string State { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string? FulfillmentIds { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static StrAutoFulfillmentProcessDEM New(int netsuiteOrderId, string state, DateTime nowUtc, string? reason = null) => new()
    {
        NetsuiteOrderId = netsuiteOrderId,
        State = state,
        Reason = reason,
        UpdatedAtUtc = nowUtc
    };
}
