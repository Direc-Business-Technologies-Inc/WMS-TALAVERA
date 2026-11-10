namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public record BulkFulfillmentNotificationDTO(Guid RequestId, int BulkFulfillmentId, long Sequence, string Status, string? Message);
