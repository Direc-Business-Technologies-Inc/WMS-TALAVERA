namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public enum BulkFulfillmentOutcome { Submitted, Rejected, Uncertain }

public record BulkFulfillmentResultDTO(BulkFulfillmentOutcome Outcome, string Message, int? ReferenceId = null)
{
    public static BulkFulfillmentResultDTO Reject(string message) => new(BulkFulfillmentOutcome.Rejected, message);
    public static BulkFulfillmentResultDTO Uncertain() => new(BulkFulfillmentOutcome.Uncertain,
        "The request may have been created. Verify Bulk Fulfillment records in NetSuite before resubmitting.");
}
