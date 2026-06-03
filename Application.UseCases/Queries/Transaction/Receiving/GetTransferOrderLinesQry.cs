using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;


public record GetTransferOrderLinesQry(int DocEntry, DataGridIntent Intent) : IRequest<(IEnumerable<ReceivingLineDTO> data, int count)>;

public class GetTransferOrderLinesQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetTransferOrderLinesQry, (IEnumerable<ReceivingLineDTO> data, int count)>
{
    public async Task<(IEnumerable<ReceivingLineDTO> data, int count)> Handle(GetTransferOrderLinesQry request, CancellationToken cancellationToken)
    {
        var (nsData, count) = await receivingIntegration.GetTransferOrderLinesAsync(request.DocEntry, request.Intent);  

        return (nsData.Adapt<IEnumerable<ReceivingLineDTO>>(), count);
    }
}
