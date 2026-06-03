using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrdersQry(DataGridIntent Intent) : IRequest<(IEnumerable<ReceivingDataGridDTO> Data, int Count)>;

public class GetPurchaseOrdersQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrdersQry, (IEnumerable<ReceivingDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ReceivingDataGridDTO> Data, int Count)> Handle(GetPurchaseOrdersQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<ReceivingInfoNSDTO> Data, int Count) = await receivingIntegration.GetPurchaseOrdersListAsync(request.Intent);

        var x = Data.Adapt<IEnumerable<ReceivingDataGridDTO>>();
        return (x, Count);
    }
}
