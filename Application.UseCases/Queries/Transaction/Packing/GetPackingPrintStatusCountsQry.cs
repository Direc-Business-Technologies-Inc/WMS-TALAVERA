using Application.DataTransferObjects.Transactions;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing;

public record GetPackingPrintStatusCountsQry(DataGridIntent Intent) : IRequest<PrintStatusCountsDTO>;

public class GetPackingPrintStatusCountsQryHandler(IReturnPackingIntegration integration)
    : IRequestHandler<GetPackingPrintStatusCountsQry, PrintStatusCountsDTO>
{
    public Task<PrintStatusCountsDTO> Handle(GetPackingPrintStatusCountsQry request, CancellationToken cancellationToken)
        => integration.GetPackingPrintStatusCounts(request.Intent, cancellationToken);
}
