using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.StockTransferRequest;


public record GetReturnsListQry(DataGridIntent Intent) : IRequest<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>;

public class GetReturnsListQryHandler(IStockTransferRequestIntegration integration)
    : IRequestHandler<GetReturnsListQry, (IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> Handle(GetReturnsListQry request, CancellationToken cancellationToken)
    {
        return integration.GetReturnsList(request.Intent);
    }
}