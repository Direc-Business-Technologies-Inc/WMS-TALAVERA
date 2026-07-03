using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryTransferRequests;

public record GetInventoryTransferRequestQry(string Ref) : IRequest<InventoryTransferRequestDTO?>;

public class GetInventoryTransferRequestQryHandler(IInventoryTransferRequestIntegration integration)
    : IRequestHandler<GetInventoryTransferRequestQry, InventoryTransferRequestDTO?>
{
    public async Task<InventoryTransferRequestDTO?> Handle(GetInventoryTransferRequestQry request, CancellationToken cancellationToken)
    {
        var headerTask = integration.GetInventoryTransferRequestAsync(request.Ref);
        var linesTask = integration.GetInventoryTransferRequestLinesAsync(request.Ref);

        await Task.WhenAll(headerTask, linesTask); //do tasks concurrently to save time
        // ideally you only wait for the headerTask and if thats null cancel linesTask 
        // but too convoluted

        var header = await headerTask;
        if (header is null) return header;

        var lines = await linesTask;
        header.Lines = [.. lines];
        return header;
    }
}
