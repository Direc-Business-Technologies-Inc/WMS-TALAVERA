using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.Returns;

public record GetReturnsQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;
public record GetPackingReturnByNumberQry(string OrderNumber, RequestPerUserDTO User) : IRequest<OrdersDTO?>;

public class GetPackingReturnByNumberQryHandler(INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackingReturnByNumberQry, OrdersDTO?>
{
    public async Task<OrdersDTO?> Handle(GetPackingReturnByNumberQry request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.User.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["ordernumber"] = request.OrderNumber.Trim()
        };
        var data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Return_x_Packing_Get_PendingFulfillment_ByOrderNumber", parameters, ct: cancellationToken);
        return data?.SingleOrDefault();
    }
}

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
