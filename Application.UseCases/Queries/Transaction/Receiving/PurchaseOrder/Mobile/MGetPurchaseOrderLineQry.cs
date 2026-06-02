using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.PurchaseOrder.Mobile;

public record MGetPurchaseOrderLineQry(PurchaseOrderLineRequestDTO order) : IRequest<IEnumerable<PurchaseOrderLineDTO>>;

public class MGetPurchaseOrderLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<MGetPurchaseOrderLineQry, IEnumerable<PurchaseOrderLineDTO>>
{
    public async Task<IEnumerable<PurchaseOrderLineDTO>> Handle(
        MGetPurchaseOrderLineQry request,
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