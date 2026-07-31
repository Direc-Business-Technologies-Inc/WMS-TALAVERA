using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrderStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseOrderStatusDTO>, int)>;

public class GetPurchaseOrderStatusesQryHandler(
    IReceivingIntegration integration
    ) : IRequestHandler<GetPurchaseOrderStatusesQry, (IEnumerable<PurchaseOrderStatusDTO>, int)>
{
    public Task<(IEnumerable<PurchaseOrderStatusDTO>, int)> Handle(GetPurchaseOrderStatusesQry request, CancellationToken cancellationToken)
    {
        return integration.GetPurchaseOrderStatuses(request.Intent);
    }
}