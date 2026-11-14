using Application.DataTransferObjects.Transactions;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetReceivingPrintStatusCountsQry(DataGridIntent Intent) : IRequest<PrintStatusCountsDTO>;

public class GetReceivingPrintStatusCountsQryHandler(IReceivingIntegration integration)
    : IRequestHandler<GetReceivingPrintStatusCountsQry, PrintStatusCountsDTO>
{
    public Task<PrintStatusCountsDTO> Handle(GetReceivingPrintStatusCountsQry request, CancellationToken cancellationToken)
        => integration.GetReceivingPrintStatusCounts(request.Intent, cancellationToken);
}
