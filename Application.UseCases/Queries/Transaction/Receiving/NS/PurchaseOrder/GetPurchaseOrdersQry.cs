using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;

public record GetPurchaseOrdersQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;

public class GetPurchaseOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPurchaseOrdersQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetPurchaseOrdersQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.user.NetsuiteUserInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_PurchaseOrder_Get_PendingReceipt", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
