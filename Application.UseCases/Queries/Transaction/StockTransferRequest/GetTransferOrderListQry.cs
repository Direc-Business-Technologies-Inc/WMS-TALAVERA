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

public record GetTransferOrderListQry(DataGridIntent Intent) : IRequest<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>;

public class GetStockTransferRequestListQryHandler(IStockTransferRequestIntegration integration) 
    : IRequestHandler<GetTransferOrderListQry, (IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> Handle(GetTransferOrderListQry request, CancellationToken cancellationToken)
    {
        return integration.GetTransferOrderList(request.Intent);
    }
}
