namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public record SubmitBulkFulfillmentDTO(IReadOnlyList<int> ReturnIds, string? Memo = null);
