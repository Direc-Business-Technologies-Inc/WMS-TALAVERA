using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.Returns;

public record GetReturnsQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;

public class GetGetReturnsQryQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetReturnsQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetReturnsQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Return_x_Packing_Get_PendingFulfillment", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}