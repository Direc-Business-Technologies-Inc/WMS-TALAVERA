using Domain.Commons;

namespace Domain.Entities.Transaction.StockTransferRequest;

/// <summary>First-activation cutoff for a category/from/to route, independent of NS rule maintenance.</summary>
public sealed class StrAutoRouteActivationDEM : EntityDEM
{
    private StrAutoRouteActivationDEM() { }

    public string RouteKey { get; private set; } = string.Empty;
    public DateTime ActivatedAtUtc { get; private set; }

    public static StrAutoRouteActivationDEM New(string routeKey, DateTime activatedAtUtc) => new()
    {
        RouteKey = routeKey,
        ActivatedAtUtc = activatedAtUtc
    };
}
