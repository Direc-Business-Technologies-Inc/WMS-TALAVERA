using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record GetPackingStockTransferRequestQry(string Ref) : IRequest<StockTransferRequestInfoPackingDTO?>;

public class GetPackingStockTransferRequestQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingStockTransferRequestQry, StockTransferRequestInfoPackingDTO?>
{
    public async Task<StockTransferRequestInfoPackingDTO?> Handle(GetPackingStockTransferRequestQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPackingStockTransferRequest(request.Ref);
    }
}
