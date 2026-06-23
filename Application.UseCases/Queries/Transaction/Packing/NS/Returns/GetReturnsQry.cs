using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.Returns;

public record GetReturnsQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetGetReturnsQryQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetReturnsQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetReturnsQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Return_x_Packing_Get_PendingFulfillment");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}