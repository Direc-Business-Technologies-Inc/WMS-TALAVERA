using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetReturnFromPurchaseOrderQry(string Ref) : IRequest<SupplierReturnDTO?>;

public class GetReturnFromPurchaseOrderQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetReturnFromPurchaseOrderQry, SupplierReturnDTO?>
{
    public async Task<SupplierReturnDTO?> Handle(GetReturnFromPurchaseOrderQry request, CancellationToken cancellationToken)
    {
        var linesTask = integration.GetReturnFromPurchaseOrderLinesAsync(request.Ref);
        var headerTask = integration.GetReturnFromPurchaseOrderAsync(request.Ref);

        await Task.WhenAny(linesTask, headerTask);

        var header = await headerTask;
        if (header is null) return null;

        var lines = await linesTask;
        header.Lines = [.. lines];
        return header;
    }
}
