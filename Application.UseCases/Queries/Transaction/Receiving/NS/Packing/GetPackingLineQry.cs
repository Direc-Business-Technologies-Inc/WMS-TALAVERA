using Application.DataTransferObjects.Transactions.ItemFulfillment.NS;
using Application.DataTransferObjects.Transactions.ItemFulfillment.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.Packing;


public record GetPackingLineQry(PackingLineRequestDTO order) : IRequest<IEnumerable<PackingLineDTO>>;

public class MGetPackingLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackingLineQry, IEnumerable<PackingLineDTO>>
{
    public async Task<IEnumerable<PackingLineDTO>> Handle(
        GetPackingLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<PackingLineDTO>("NS_TO_x_Itemfulfillment_Get_Items", parameters);

        return Data.Adapt<IEnumerable<PackingLineDTO>>();
    }
}