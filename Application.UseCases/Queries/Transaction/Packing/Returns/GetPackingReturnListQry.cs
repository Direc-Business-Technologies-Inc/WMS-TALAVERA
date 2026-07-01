using Application.DataTransferObjects.Transactions.Packing.Returns;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.Returns;

public record GetPackingReturnListQry(DataGridIntent Intent)
    : IRequest<(IEnumerable<ReturnsDataGridDTO> Data, int Count)>;

public class GetPackingReturnListQryHandler(IReturnPackingIntegration integration)
    : IRequestHandler<GetPackingReturnListQry, (IEnumerable<ReturnsDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<ReturnsDataGridDTO> Data, int Count)> Handle(
        GetPackingReturnListQry request,
        CancellationToken cancellationToken)
    {
        return integration.GetPackingReturnsList(request.Intent);
    }
}
