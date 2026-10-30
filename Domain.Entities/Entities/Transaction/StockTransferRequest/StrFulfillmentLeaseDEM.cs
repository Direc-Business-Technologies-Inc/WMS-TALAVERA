using Domain.Commons;

namespace Domain.Entities.Transaction.StockTransferRequest;

/// <summary>A short, database-backed exclusion lease shared by manual and automatic WMS IF writers.</summary>
public sealed class StrFulfillmentLeaseDEM : EntityDEM
{
    private StrFulfillmentLeaseDEM() { }

    public int NetsuiteOrderId { get; private set; }
    public Guid? OwnerToken { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public static StrFulfillmentLeaseDEM New(int netsuiteOrderId, Guid ownerToken, DateTime expiresAtUtc) => new()
    {
        NetsuiteOrderId = netsuiteOrderId,
        OwnerToken = ownerToken,
        ExpiresAtUtc = expiresAtUtc
    };
}
