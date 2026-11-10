using Application.DataTransferObjects.Transactions.Packing.Returns;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.Returns;

public record GetPackingReturnListQry(DataGridIntent Intent, int SubsidiaryId, bool IncludeReceived = false)
    : IRequest<(IEnumerable<ReturnsDataGridDTO> Data, int Count)>;

public class GetPackingReturnListQryHandler(IReturnPackingIntegration integration)
    : IRequestHandler<GetPackingReturnListQry, (IEnumerable<ReturnsDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<ReturnsDataGridDTO> Data, int Count)> Handle(
        GetPackingReturnListQry request,
        CancellationToken cancellationToken)
    {
        var intent = request.Intent.Snapshot();
        if (cancellationToken.CanBeCanceled) intent.CancellationToken = cancellationToken;
        return integration.GetPackingReturnsList(intent, request.SubsidiaryId, request.IncludeReceived);
    }
}
