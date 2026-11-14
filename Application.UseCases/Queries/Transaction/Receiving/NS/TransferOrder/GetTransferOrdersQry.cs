using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;

public record GetTransferOrdersQry(RequestPerUserDTO user, int limit = 50, int offset = 0) : IRequest<IEnumerable<OrdersDTO>>;
public record GetReceivingTransferOrderByNumberQry(string OrderNumber, RequestPerUserDTO User) : IRequest<OrdersDTO?>;

public class GetReceivingTransferOrderByNumberQryHandler(INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetReceivingTransferOrderByNumberQry, OrdersDTO?>
{
    public async Task<OrdersDTO?> Handle(GetReceivingTransferOrderByNumberQry request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.User.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.User.NetsuiteUserInternalId.ToString(),
            ["ordernumber"] = request.OrderNumber.Trim()
        };
        var data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TransferOrder_Get_PendingReceipt_ByOrderNumber", parameters, ct: cancellationToken);
        return data?.SingleOrDefault();
    }
}

public class GetTransferOrdersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrdersQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetTransferOrdersQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.user.NetsuiteUserInternalId.ToString()
        };

        const int maxPageSize = 100;
        var limit = Math.Clamp(request.limit, 1, maxPageSize);
        var offset = Math.Max(0, request.offset);
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TransferOrder_Get_PendingReceipt", parameters, limit, offset, cancellationToken);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
