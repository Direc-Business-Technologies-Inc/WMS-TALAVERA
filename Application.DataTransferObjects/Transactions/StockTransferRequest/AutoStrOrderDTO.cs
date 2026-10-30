namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public sealed record AutoStrOrderDTO(
    int NetsuiteOrderId,
    string OrderNumber,
    int TransferCategoryId,
    int FromLocationId,
    int ToLocationId,
    int SubsidiaryId,
    int ApprovalStatusId,
    DateTime ApprovedAtUtc);
