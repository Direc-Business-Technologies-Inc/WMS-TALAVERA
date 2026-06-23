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

public record GetSupplierReturnsDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<SupplierReturnDataGridDTO> data, int count)>;

public class GetSupplierReturnsDataGridQryHandler(
    ISupplierReturnIntegration integration
    ) : IRequestHandler<GetSupplierReturnsDataGridQry, (IEnumerable<SupplierReturnDataGridDTO> data, int count)>
{
    public async Task<(IEnumerable<SupplierReturnDataGridDTO> data, int count)> Handle(GetSupplierReturnsDataGridQry request, CancellationToken cancellationToken)
    {
        return await integration.GetReturnsDataGridAsync(request.Intent);
    }
}
