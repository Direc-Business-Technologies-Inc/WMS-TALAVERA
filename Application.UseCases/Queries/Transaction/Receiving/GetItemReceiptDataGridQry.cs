using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetItemReceiptDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<ItemReceiptDataGridDTO> data, int count)>;

public class GetItemReceiptDataGridQryHandler(
    IReceivingIntegration integration)
    : IRequestHandler<GetItemReceiptDataGridQry, (IEnumerable<ItemReceiptDataGridDTO> data, int count)>
{
    public Task<(IEnumerable<ItemReceiptDataGridDTO> data, int count)> Handle(GetItemReceiptDataGridQry request, CancellationToken cancellationToken)
    {
        return integration.GetItemReceiptsDatagrid(request.Intent);
    }
}