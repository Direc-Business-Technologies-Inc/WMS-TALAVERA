using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting.NS;

public record GetStartedInventoryCountingLineQry(InventoryCountingLineRequestDTO order) : IRequest<IEnumerable<InventoryCountingLineDTO>>;

public class MGetStartedInventoryCountingLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetStartedInventoryCountingLineQry, IEnumerable<InventoryCountingLineDTO>>
{
    public async Task<IEnumerable<InventoryCountingLineDTO>> Handle(
        GetStartedInventoryCountingLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<InventoryCountingLineDTO>("NS_InventoryCounting_Get_Items", parameters);

        return Data.Adapt<IEnumerable<InventoryCountingLineDTO>>();
    }
}