using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Transactions.Commons;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetMerchandiseItemsQry(DataGridIntent intent) : IRequest<(IEnumerable<ItemDTO> Data, int Count)>;

public class GetMerchandiseItemsQryHandler(IItemMasterDataIntegration itemMasterDataIntegration) : IRequestHandler<GetMerchandiseItemsQry, (IEnumerable<ItemDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ItemDTO> Data, int Count)> Handle(GetMerchandiseItemsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<ItemSelectionSAPDTO> Data, int Count) = await itemMasterDataIntegration.GetMerchandiseItems(request.intent);

        return (Data.Adapt<IEnumerable<ItemDTO>>(), Count);
    }
}
