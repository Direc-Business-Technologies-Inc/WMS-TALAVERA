using Application.DataTransferObjects.Transactions.Packing.Returns;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.Returns;

public record GetPackingReturnQry(string Ref) : IRequest<ReturnsInfoDTO?>;

public class GetPackingReturnQryHandler(IReturnPackingIntegration integration)
    : IRequestHandler<GetPackingReturnQry, ReturnsInfoDTO?>
{
    public async Task<ReturnsInfoDTO?> Handle(GetPackingReturnQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPackingReturn(request.Ref);
    }
}
