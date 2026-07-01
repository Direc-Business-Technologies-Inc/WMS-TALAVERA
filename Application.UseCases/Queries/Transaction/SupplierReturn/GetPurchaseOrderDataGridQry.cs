using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetPurchaseOrderDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseOrderDataGridDTO>, int)>;

public class GetPurchaseOrderDataGridQryHandler(ISupplierReturnIntegration integ)
    : IRequestHandler<GetPurchaseOrderDataGridQry, (IEnumerable<PurchaseOrderDataGridDTO>, int)>
{
    public Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> Handle(GetPurchaseOrderDataGridQry request, CancellationToken cancellationToken)
    {
        return integ.GetPurchaseOrdersListAsync(request.Intent);
    }
}