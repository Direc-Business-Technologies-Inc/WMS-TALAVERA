using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.TripTicket.NS;

public record GetPackedItemFulfillmentsQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetPackedItemFulfillmentsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackedItemFulfillmentsQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetPackedItemFulfillmentsQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_ItemFulfillment_Get_Packed");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}