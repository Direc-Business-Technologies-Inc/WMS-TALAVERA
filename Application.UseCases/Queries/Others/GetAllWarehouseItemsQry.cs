using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Transactions.Commons;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetAllWarehouseItemsQry(DataGridIntent Intent, string WhsCode) : IRequest<(IEnumerable<ItemDTO> Data, int Count)>;

public class GetAllWarehouseItemsQryHandler(
    IItemMasterDataIntegration itemMasterDataIntegration)
    : IRequestHandler<GetAllWarehouseItemsQry, (IEnumerable<ItemDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ItemDTO> Data, int Count)> Handle(GetAllWarehouseItemsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<ItemSelectionSAPDTO> Data, int Count) = await itemMasterDataIntegration.GetWarehouseItems(request.Intent, request.WhsCode);

        return (Data.Adapt<IEnumerable<ItemDTO>>(), Count);
    }
}
