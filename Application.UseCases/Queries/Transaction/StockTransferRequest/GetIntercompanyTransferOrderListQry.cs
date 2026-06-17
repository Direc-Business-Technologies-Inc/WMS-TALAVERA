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

public record GetIntercompanyTransferOrderListQry(DataGridIntent Intent) : IRequest<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>;

public class GetIntercompanyTransferOrderListQryHandler(IStockTransferRequestIntegration integration)
    : IRequestHandler<GetIntercompanyTransferOrderListQry, (IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> Handle(GetIntercompanyTransferOrderListQry request, CancellationToken cancellationToken)
    {
        return integration.GetIntercompanyTransferOrderList(request.Intent);
    }
}