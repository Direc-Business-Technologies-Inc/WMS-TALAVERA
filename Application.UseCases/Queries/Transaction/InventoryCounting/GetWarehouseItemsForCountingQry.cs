using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting;

public record GetWarehouseItemsForCountingQry(string WhsCode)
    : IRequest<IEnumerable<InventoryCountingItemSAPDTO>>;

public class GetWarehouseItemsForCountingQryHandler(IItemMasterDataIntegration Items)
    : IRequestHandler<GetWarehouseItemsForCountingQry, IEnumerable<InventoryCountingItemSAPDTO>>
{
    public async Task<IEnumerable<InventoryCountingItemSAPDTO>> Handle(
        GetWarehouseItemsForCountingQry request,
        CancellationToken cancellationToken)
        => await Items.GetWarehouseItemsForCounting(request.WhsCode);
}
