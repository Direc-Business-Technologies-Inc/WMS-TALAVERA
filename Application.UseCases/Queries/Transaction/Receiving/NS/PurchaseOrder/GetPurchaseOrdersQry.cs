using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;

public record GetPurchaseOrdersQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetPurchaseOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPurchaseOrdersQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetPurchaseOrdersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_PurchaseOrder_Get_PendingReceipt");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
