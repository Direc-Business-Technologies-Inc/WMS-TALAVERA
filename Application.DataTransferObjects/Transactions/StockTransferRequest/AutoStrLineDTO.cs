using Application.DataTransferObjects.Transactions.Commons.NS;

namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

/// <summary>Authoritative NS line and stock read used to build an unattended IF.</summary>
public sealed record AutoStrLineDTO(
    AutoFulfillmentStockLineDTO Stock,
    PostTransferOrderDTO Posting);
