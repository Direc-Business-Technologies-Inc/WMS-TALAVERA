using Application.DataTransferObjects.Transactions.SupplierReturn;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;

public interface IBulkFulfillmentNotificationsHandler
{
    Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> GetUnreadAsync(CancellationToken ct = default);
    Task DismissAsync(Guid requestId, long sequence, CancellationToken ct = default);
}
