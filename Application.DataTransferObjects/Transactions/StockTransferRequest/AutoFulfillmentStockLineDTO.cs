namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

/// <summary>
/// A fresh, authoritative NetSuite order line and its available stock in base
/// units. The caller must resolve the inventory condition before constructing
/// this DTO; it must never infer Good/Bad from the return category alone.
/// </summary>
public sealed record AutoFulfillmentStockLineDTO(
    int LineId,
    int ItemId,
    int LocationId,
    int? InventoryStatusId,
    int? BinId,
    bool? RequiresBin,
    decimal OrderedBaseQuantity,
    decimal AlreadyShippedBaseQuantity,
    decimal? AvailableBaseQuantity,
    decimal UomRate,
    bool IsClosed);
