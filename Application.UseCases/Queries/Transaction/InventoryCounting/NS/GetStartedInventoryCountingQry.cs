using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting.NS;

public record GetStartedInventoryCountingQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetStartedInventoryCountingQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetStartedInventoryCountingQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetStartedInventoryCountingQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_InventoryCounting_Get_Started");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}