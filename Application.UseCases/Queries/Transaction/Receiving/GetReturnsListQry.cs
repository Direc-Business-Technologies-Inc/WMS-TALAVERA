using Application.DataTransferObjects.Transactions.Receiving;
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

public record GetReturnsListQry(DataGridIntent Intent) : IRequest<(IEnumerable<ReceivingDataGridDTO> Data, int Count)>;

public class GetReturnsListQryHandler(IReceivingIntegration receivingIntegration) : IRequestHandler<GetReturnsListQry, (IEnumerable<ReceivingDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ReceivingDataGridDTO> Data, int Count)> Handle(GetReturnsListQry request, CancellationToken cancellationToken)
    {
        (var data, var count) = await receivingIntegration.GetReturnsListAsync(request.Intent);
        return (data.Adapt<IEnumerable<ReceivingDataGridDTO>>(), count);
    }
}