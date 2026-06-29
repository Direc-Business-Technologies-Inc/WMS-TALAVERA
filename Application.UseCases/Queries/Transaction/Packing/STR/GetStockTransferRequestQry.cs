using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record GetPackingStockTransferRequestQry(string eid) : IRequest<StockTransferRequestInfoPackingDTO>;

public class GetPackingStockTransferRequestQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingStockTransferRequestQry, StockTransferRequestInfoPackingDTO>
{
    public async Task<StockTransferRequestInfoPackingDTO> Handle(GetPackingStockTransferRequestQry request, CancellationToken cancellationToken)
    {
        var header = await integration.GetPackingStockTransferRequest(request.eid);
        if (header is null) throw new Exception($"Couldnt find packing stock transfer request with id {request.eid}");

        var lines = await integration.GetPackingStockTransferRequestLines(request.eid);
        header.Lines = [.. lines ?? []];

        return header;
    }
}