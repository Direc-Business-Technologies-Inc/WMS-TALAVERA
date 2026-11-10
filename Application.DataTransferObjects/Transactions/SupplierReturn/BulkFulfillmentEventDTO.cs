namespace Application.DataTransferObjects.Transactions.SupplierReturn;

// Sequence increases for each status change, including retries of the same BF.
public record BulkFulfillmentEventDTO(Guid EventId, int BulkFulfillmentId, long Sequence, string Status, string? Message = null);
