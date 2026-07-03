using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.StockTransferRequest;

public record GetStockTransferRequestQry(string eid) : IRequest<StockTransferRequestInfoDTO>;

public class GetStockTransferRequestQryHandler(IStockTransferRequestIntegration integration)
    : IRequestHandler<GetStockTransferRequestQry, StockTransferRequestInfoDTO>
{
    public async Task<StockTransferRequestInfoDTO> Handle(GetStockTransferRequestQry request, CancellationToken cancellationToken)
    {
        var header = await integration.GetStockTransferRequest(request.eid);
        if (header is null) throw new Exception($"Couldnt find stock transfer request with id {request.eid}");

        var lines = await integration.GetStockTransferRequestLines(request.eid);
        header.Lines = [.. lines ?? []];

        return header;
    }
}