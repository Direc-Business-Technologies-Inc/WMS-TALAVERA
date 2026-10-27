using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

public record GetTransferOrdersQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;
public record GetPackingTransferOrderByNumberQry(string OrderNumber, RequestPerUserDTO User) : IRequest<OrdersDTO?>;

public class GetPackingTransferOrderByNumberQryHandler(INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackingTransferOrderByNumberQry, OrdersDTO?>
{
    public async Task<OrdersDTO?> Handle(GetPackingTransferOrderByNumberQry request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.User.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.User.NetsuiteUserInternalId.ToString(),
            ["ordernumber"] = request.OrderNumber.Trim()
        };
        var data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Packing_Get_PendingFulfillment_ByOrderNumber", parameters, ct: cancellationToken);
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
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_TO_x_Packing_Get_PendingFulfillment", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
