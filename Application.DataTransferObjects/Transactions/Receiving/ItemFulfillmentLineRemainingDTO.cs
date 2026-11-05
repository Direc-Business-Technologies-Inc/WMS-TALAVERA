namespace Application.DataTransferObjects.Transactions.Receiving;

/// <summary>
/// Transfer-order line quantities allocated to one Item Fulfillment. The
/// received quantity is fulfillment-scoped, unlike the Transfer Order's
/// quantityshiprecv counter which can include receipts against other IFs.
/// </summary>
public sealed class ItemFulfillmentLineRemainingDTO
{
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityFulfilled { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityRemaining => Math.Min(QuantityPlanned, QuantityFulfilled) - QuantityReceived;
}
