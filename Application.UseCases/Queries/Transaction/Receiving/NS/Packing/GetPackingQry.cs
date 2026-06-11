using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.Packing;
public record GetPackingQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetPackingQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackingQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetPackingQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Itemfulfillment_Get_PendingFulfillment");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}