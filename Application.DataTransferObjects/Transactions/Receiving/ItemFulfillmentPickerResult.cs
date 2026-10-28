namespace Application.DataTransferObjects.Transactions.Receiving;

/// <summary>Verified picker rows plus the number of candidates withheld for verification failures.</summary>
public sealed record ItemFulfillmentPickerResult(
    IEnumerable<ItemFulfillmentDTO> Items,
    int TotalResults,
    int SkippedUnverifiableCount);
