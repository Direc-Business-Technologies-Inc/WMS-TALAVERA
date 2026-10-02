using Application.DataTransferObjects.Others.NS;

namespace Application.UseCases.Commands.Transaction.Packing.NS;

internal static class ItemFulfillmentRequestValidator
{
    internal static bool TryGetSourceTransactionId<T>(
        IReadOnlyCollection<T>? lines,
        out int sourceTransactionId,
        out string errorMessage)
        where T : TransactionDTO
    {
        sourceTransactionId = 0;

        if (lines is null || lines.Count == 0)
        {
            errorMessage = "At least one fulfillment line is required.";
            return false;
        }

        var transactionIds = lines
            .Select(line => line.NetsuiteOrderInternalId)
            .Distinct()
            .Take(2)
            .ToArray();

        if (transactionIds.Length != 1 || transactionIds[0] <= 0)
        {
            errorMessage = "All fulfillment lines must belong to one valid source transaction.";
            return false;
        }

        sourceTransactionId = transactionIds[0];
        errorMessage = string.Empty;
        return true;
    }
}
