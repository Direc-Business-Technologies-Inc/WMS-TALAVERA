using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetBulkFulfillmentNotificationsQry(Guid UserId) : IRequest<IReadOnlyList<BulkFulfillmentNotificationDTO>>;

public class GetBulkFulfillmentNotificationsQryHandler(IBulkFulfillmentTrackingRepository tracking)
    : IRequestHandler<GetBulkFulfillmentNotificationsQry, IReadOnlyList<BulkFulfillmentNotificationDTO>>
{
    public Task<IReadOnlyList<BulkFulfillmentNotificationDTO>> Handle(GetBulkFulfillmentNotificationsQry request, CancellationToken cancellationToken) =>
        request.UserId == Guid.Empty ? Task.FromResult<IReadOnlyList<BulkFulfillmentNotificationDTO>>([])
            : tracking.GetUnreadAsync(request.UserId, cancellationToken);
}
