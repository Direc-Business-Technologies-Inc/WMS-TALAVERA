using Application.DataTransferObjects.Transactions.Packing;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IItemFulfillmentStatusResolver
{
    Task<ItemFulfillmentShipStatus> ResolveAsync(
        int sourceTransactionId,
        CancellationToken cancellationToken = default);
}
