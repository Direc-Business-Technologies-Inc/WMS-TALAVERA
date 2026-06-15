using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetReturnsQry(string entry) : IRequest<ReturnsDTO?>;

public class GetReturnsQryHandler(IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetReturnsQry, ReturnsDTO?>
{
    public async Task<ReturnsDTO?> Handle(GetReturnsQry request, CancellationToken cancellationToken)
    {
        var header = await receivingIntegration.GetReturnsHeaderAsync(request.entry);
        if (header == null) return null;

        var lines = await receivingIntegration.GetReturnsLinesAsync(request.entry);

        header.Lines = [.. lines];
        return header;
    }
}
