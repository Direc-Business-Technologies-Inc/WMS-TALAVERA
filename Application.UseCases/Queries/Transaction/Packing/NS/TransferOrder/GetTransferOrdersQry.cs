using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

public record GetTransferOrdersQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetTransferOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrdersQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetTransferOrdersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Packing_Get_PendingFulfillment");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}