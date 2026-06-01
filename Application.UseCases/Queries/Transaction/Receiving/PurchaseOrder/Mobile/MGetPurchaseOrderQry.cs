using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using PurchaseOrderDTO = Application.DataTransferObjects.Others.NS.PurchaseOrderDTO;

namespace Application.UseCases.Queries.Transaction.Receiving.PurchaseOrder.Mobile;

public record MGetPurchaseOrdersQry() : IRequest<IEnumerable<PurchaseOrderDTO>>;

public class MGetPurchaseOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<MGetPurchaseOrdersQry, IEnumerable<PurchaseOrderDTO>>
{
    public async Task<IEnumerable<PurchaseOrderDTO>> Handle(
        MGetPurchaseOrdersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.GetAllPOPendingReceipt();

        return Data.Adapt<IEnumerable<PurchaseOrderDTO>>();
    }
}
