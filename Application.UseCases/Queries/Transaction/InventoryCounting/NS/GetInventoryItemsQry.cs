using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting.NS;

public record GetInventoryItemsQry() : IRequest<IEnumerable<InventoryItemDTO>>;

public class GetInventoryItemsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetInventoryItemsQry, IEnumerable<InventoryItemDTO>>
{
    public async Task<IEnumerable<InventoryItemDTO>> Handle(
        GetInventoryItemsQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<InventoryItemDTO>("NS_Get_InventoryItems");
        return Data.Adapt<IEnumerable<InventoryItemDTO>>();
    }
}