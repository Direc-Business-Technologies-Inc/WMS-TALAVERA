using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetItemPerBarcodeQry(string barcode) : IRequest<IEnumerable<InventoryItemDTO>>;

public class GetItemPerBarcodeQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetItemPerBarcodeQry, IEnumerable<InventoryItemDTO>>
{
    public async Task<IEnumerable<InventoryItemDTO>> Handle(
        GetItemPerBarcodeQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["barcode"] = request.barcode
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<InventoryItemDTO>("NS_Item_Get_PerBarcode", parameters);

        return Data.Adapt<IEnumerable<InventoryItemDTO>>();
    }
}