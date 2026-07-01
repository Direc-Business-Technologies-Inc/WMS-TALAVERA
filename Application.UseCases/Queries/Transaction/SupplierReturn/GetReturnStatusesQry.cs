using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetReturnStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<ReturnStatusDTO> data, int count)>;

public class GetReturnStatusesQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetReturnStatusesQry, (IEnumerable<ReturnStatusDTO> data, int count)>
{
    public async Task<(IEnumerable<ReturnStatusDTO> data, int count)> Handle(GetReturnStatusesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetReturnStatuses(request.Intent);
    }
}
