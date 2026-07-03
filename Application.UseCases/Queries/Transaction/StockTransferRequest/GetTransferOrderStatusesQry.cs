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

public record GetTransferOrderStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<TransferOrderStatus>, int count)>;

public class GetTransferOrderStatusesQryHandler(IStockTransferRequestIntegration integration)
    : IRequestHandler<GetTransferOrderStatusesQry, (IEnumerable<TransferOrderStatus>, int count)>
{
    public async Task<(IEnumerable<TransferOrderStatus>, int count)> Handle(GetTransferOrderStatusesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetTransferOrderStatuses(request.Intent);
    }
}