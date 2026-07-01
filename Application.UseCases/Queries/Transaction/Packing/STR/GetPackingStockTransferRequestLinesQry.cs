using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record GetPackingStockTransferRequestLinesQry(string Ref, DataGridIntent Intent)
    : IRequest<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)>;

public class GetPackingStockTransferRequestLinesQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingStockTransferRequestLinesQry, (IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)>
{
    public async Task<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)> Handle(
        GetPackingStockTransferRequestLinesQry request,
        CancellationToken cancellationToken)
    {
        return await integration.GetPackingStockTransferRequestLines(request.Ref, request.Intent);
    }
}
