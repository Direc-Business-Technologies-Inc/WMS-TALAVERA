using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;

public record GetTransferOrderIFQry(TransferOrderIFRequestDTO id) : IRequest<IEnumerable<OrdersDTO>>;

public class GetTransferOrderIFQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrderIFQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetTransferOrderIFQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["id"] = request.id.NetsuiteOrderInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_Get_Itemfulfillments", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}