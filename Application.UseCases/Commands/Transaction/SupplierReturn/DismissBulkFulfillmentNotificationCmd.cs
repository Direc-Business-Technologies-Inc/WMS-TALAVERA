using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record DismissBulkFulfillmentNotificationCmd(Guid UserId, Guid RequestId, long Sequence) : IRequest;

public class DismissBulkFulfillmentNotificationCmdHandler(IBulkFulfillmentTrackingRepository tracking)
    : IRequestHandler<DismissBulkFulfillmentNotificationCmd>
{
    public Task Handle(DismissBulkFulfillmentNotificationCmd request, CancellationToken cancellationToken) =>
        request.UserId == Guid.Empty || request.RequestId == Guid.Empty || request.Sequence <= 0 ? Task.CompletedTask
            : tracking.DismissAsync(request.UserId, request.RequestId, request.Sequence, cancellationToken);
}
