using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;

public record GetPurchaseOrderLineQry(PurchaseOrderLineRequestDTO order) : IRequest<IEnumerable<PurchaseOrderLineDTO>>;

public class MGetPurchaseOrderLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPurchaseOrderLineQry, IEnumerable<PurchaseOrderLineDTO>>
{
    public async Task<IEnumerable<PurchaseOrderLineDTO>> Handle(
        GetPurchaseOrderLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<PurchaseOrderLineDTO>("NS_PurchaseOrder_Get_Items", parameters);

        return Data.Adapt<IEnumerable<PurchaseOrderLineDTO>>();
    }
}