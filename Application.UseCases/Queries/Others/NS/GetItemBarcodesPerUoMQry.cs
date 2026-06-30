using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetItemBarcodesPerUoMQry(List<ItemBarcodesRequestDTO> items) : IRequest<IEnumerable<ItemBarcodesPerUoMDTO>>;

public class GetItemBarcodesPerUoMQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetItemBarcodesPerUoMQry, IEnumerable<ItemBarcodesPerUoMDTO>>
{
    public async Task<IEnumerable<ItemBarcodesPerUoMDTO>> Handle(
        GetItemBarcodesPerUoMQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["items"] = string.Join(",", request.items.Select(x => x.NetsuiteMaterialInternalId))
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<ItemBarcodesPerUoMDTO>("NS_Item_Get_Barcodes", parameters);

        return Data.Adapt<IEnumerable<ItemBarcodesPerUoMDTO>>();
    }
}