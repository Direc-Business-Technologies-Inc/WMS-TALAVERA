namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public sealed record StrAutoFulfillmentOutcomeDTO(
    int NetsuiteOrderId,
    string State,
    string? Reason,
    string? FulfillmentIds,
    DateTime UpdatedAtUtc);
