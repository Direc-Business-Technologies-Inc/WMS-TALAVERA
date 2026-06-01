using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Transactions.Commons;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetItemsInWarehouseQry(DataGridIntent Intent, string WhsCode, List<string> ItemCodes) : IRequest<(IEnumerable<ItemDTO> Data, int Count)>;

public class GetItemsInWarehouseQryHandler(
    IItemMasterDataIntegration itemMasterDataIntegration)
    : IRequestHandler<GetItemsInWarehouseQry, (IEnumerable<ItemDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ItemDTO> Data, int Count)> Handle(GetItemsInWarehouseQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<ItemSelectionSAPDTO> Data, int Count) = await itemMasterDataIntegration.GetItemWarehouseLevel(request.Intent, request.WhsCode, request.ItemCodes);

        return (Data.Adapt<IEnumerable<ItemDTO>>(), Count);
    }
}
