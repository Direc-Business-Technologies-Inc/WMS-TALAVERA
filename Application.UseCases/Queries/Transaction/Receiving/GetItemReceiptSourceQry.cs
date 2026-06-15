using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetItemReceiptSourceQry(string Ref) : IRequest<ItemReceiptDTO?>;

public class GetItemReceiptSourceQryHandler(
    IReceivingIntegration receivingIntegration) : IRequestHandler<GetItemReceiptSourceQry, ItemReceiptDTO?>
{
    public async Task<ItemReceiptDTO?> Handle(GetItemReceiptSourceQry request, CancellationToken cancellationToken)
    {
        var header = await receivingIntegration.GetItemReceiptHeaderAsync(request.Ref);
        if (header == null) return null;

        var isTransferOrder = header.Type.Equals("Returns", StringComparison.OrdinalIgnoreCase) || header.Type.Equals("TrnfrOrd", StringComparison.OrdinalIgnoreCase);
        var lines = await receivingIntegration.GetItemReceiptLinesAsync(request.Ref, isTransferOrder);
        header.Lines = [..lines];

        return header;
    }
}