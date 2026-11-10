using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Queries.Transaction.SupplierReturn;
using MediatR;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;

public class BulkFulfillmentNotificationsHandler(AppAuthenticationService auth, ISender sender) : IBulkFulfillmentNotificationsHandler
{
    private Guid UserId => auth.CurrentUser.Identity?.IsAuthenticated == true
        && Guid.TryParse(auth.CurrentUser.FindFirst("Id")?.Value, out var id) ? id : Guid.Empty;

    public Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> GetUnreadAsync(CancellationToken ct = default) =>
        sender.Send(new GetBulkFulfillmentNotificationsQry(UserId), ct);

    public Task DismissAsync(Guid requestId, long sequence, CancellationToken ct = default) =>
        sender.Send(new DismissBulkFulfillmentNotificationCmd(UserId, requestId, sequence), ct);
}
