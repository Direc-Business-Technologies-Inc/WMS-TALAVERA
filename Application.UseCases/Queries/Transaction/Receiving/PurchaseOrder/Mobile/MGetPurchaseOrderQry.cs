using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Application.DataTransferObjects.Others.NS;

namespace Application.UseCases.Queries.Transaction.Receiving.PurchaseOrder.Mobile;

public record MGetPurchaseOrdersQry() : IRequest<IEnumerable<OrdersDTO>>;

public class MGetPurchaseOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<MGetPurchaseOrdersQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        MGetPurchaseOrdersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_PurchaseOrder_Get_PendingReceipt");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
