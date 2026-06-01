using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrdersQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseOrderDataGridDTO> Data, int Count)>;

public class GetPurchaseOrdersQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrdersQry, (IEnumerable<PurchaseOrderDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<PurchaseOrderDataGridDTO> Data, int Count)> Handle(GetPurchaseOrdersQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<PurchaseOrderSAPDTO> Data, int Count) = await receivingIntegration.GetPurchaseOrdersListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<PurchaseOrderDataGridDTO>>(), Count);
    }
}
