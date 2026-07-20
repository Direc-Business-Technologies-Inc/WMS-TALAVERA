using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetTransferOrderStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<TransferOrderStatusDTO>, int)>;

public class GetTransferOrderStatusesQryHandler(
    IReceivingIntegration integration
    ) : IRequestHandler<GetTransferOrderStatusesQry, (IEnumerable<TransferOrderStatusDTO>, int)>
{
    public Task<(IEnumerable<TransferOrderStatusDTO>, int)> Handle(GetTransferOrderStatusesQry request, CancellationToken cancellationToken)
    {
        return integration.GetTransferOrderStatuses(request.Intent);
    }
}
