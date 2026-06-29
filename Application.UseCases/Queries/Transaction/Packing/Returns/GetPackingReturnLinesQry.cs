using Application.DataTransferObjects.Transactions.Packing.Returns;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.Returns;

public record GetPackingReturnLinesQry(string Ref, DataGridIntent Intent)
    : IRequest<(IEnumerable<ReturnsLineDTO> Data, int Count)>;

public class GetPackingReturnLinesQryHandler(IReturnPackingIntegration integration)
    : IRequestHandler<GetPackingReturnLinesQry, (IEnumerable<ReturnsLineDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ReturnsLineDTO> Data, int Count)> Handle(
        GetPackingReturnLinesQry request,
        CancellationToken cancellationToken)
    {
        return await integration.GetPackingReturnLines(request.Ref, request.Intent);
    }
}
