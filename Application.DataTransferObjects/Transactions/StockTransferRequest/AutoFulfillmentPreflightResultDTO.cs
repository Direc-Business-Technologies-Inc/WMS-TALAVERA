namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public enum AutoFulfillmentPreflightDecision
{
    Ready,
    NoOpenLines,
    Blocked
}

public sealed record AutoFulfillmentPreflightResultDTO(
    AutoFulfillmentPreflightDecision Decision,
    IReadOnlyDictionary<int, decimal> RemainingBaseQuantityByLine,
    string Reason);
