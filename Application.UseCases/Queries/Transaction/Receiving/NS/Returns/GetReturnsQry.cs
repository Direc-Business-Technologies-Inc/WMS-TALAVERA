using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.Returns;

public record GetReturnsQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;
public record GetReceivingReturnByNumberQry(string OrderNumber, RequestPerUserDTO User) : IRequest<OrdersDTO?>;

public class GetReceivingReturnByNumberQryHandler(INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetReceivingReturnByNumberQry, OrdersDTO?>
{
    public async Task<OrdersDTO?> Handle(GetReceivingReturnByNumberQry request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.User.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.User.NetsuiteUserInternalId.ToString(),
            ["ordernumber"] = request.OrderNumber.Trim()
        };
        var data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TransferOrder_x_Return_Get_PendingReceipt_ByOrderNumber", parameters, ct: cancellationToken);
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
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.user.NetsuiteUserInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TransferOrder_x_Return_Get_PendingReceipt", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
