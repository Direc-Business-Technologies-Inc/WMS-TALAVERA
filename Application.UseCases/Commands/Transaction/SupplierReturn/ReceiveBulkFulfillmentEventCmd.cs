using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record ReceiveBulkFulfillmentEventCmd(BulkFulfillmentEventDTO Data) : IRequest<BulkFulfillmentEventReceipt>;

public class ReceiveBulkFulfillmentEventCmdHandler(IBulkFulfillmentTrackingRepository tracking)
    : IRequestHandler<ReceiveBulkFulfillmentEventCmd, BulkFulfillmentEventReceipt>
{
    public Task<BulkFulfillmentEventReceipt> Handle(ReceiveBulkFulfillmentEventCmd request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        if (data.EventId == Guid.Empty || data.BulkFulfillmentId <= 0 || data.Sequence <= 0
            || data.Status is not ("Done" or "Failed") || data.Message?.Length > 4000)
            return Task.FromResult(BulkFulfillmentEventReceipt.Invalid);
        return tracking.ReceiveAsync(data, cancellationToken);
    }
}
