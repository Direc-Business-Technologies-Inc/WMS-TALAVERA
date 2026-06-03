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

public record GetTransferOrdersQry(DataGridIntent Intent): IRequest<(IEnumerable<ReceivingDataGridDTO> Data, int Count)>;

public class GetTransferOrdersQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetTransferOrdersQry, (IEnumerable<ReceivingDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ReceivingDataGridDTO> Data, int Count)> Handle(GetTransferOrdersQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<ReceivingInfoNSDTO> Data, int Count) = await receivingIntegration.GetTransferOrderListAsync(request.Intent);

        var x = Data.Adapt<IEnumerable<ReceivingDataGridDTO>>();

        return (x, Count);
    }
}
