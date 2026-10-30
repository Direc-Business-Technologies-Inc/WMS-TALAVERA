namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public sealed record StrAutoFulfillmentResultDTO(int NetsuiteOrderId, string State, string Reason);
