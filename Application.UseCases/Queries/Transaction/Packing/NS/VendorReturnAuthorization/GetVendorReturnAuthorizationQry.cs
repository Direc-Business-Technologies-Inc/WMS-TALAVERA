using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;

public record GetVendorReturnAuthorizationQry(RequestPerUserDTO user) : IRequest<IEnumerable<OrdersDTO>>;
public record GetPackingVendorReturnByNumberQry(string OrderNumber, RequestPerUserDTO User) : IRequest<OrdersDTO?>;

public class GetPackingVendorReturnByNumberQryHandler(INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackingVendorReturnByNumberQry, OrdersDTO?>
{
    public async Task<OrdersDTO?> Handle(GetPackingVendorReturnByNumberQry request, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.User.NetsuiteUserSubsidiaryInternalId.ToString(),
            ["userid"] = request.User.NetsuiteUserInternalId.ToString(),
            ["ordernumber"] = request.OrderNumber.Trim()
        };
        var data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_VendorReturnAuthorization_Get_PendingReturn_ByOrderNumber", parameters, ct: cancellationToken);
        return data?.SingleOrDefault();
    }
}

public class GetVendorReturnAuthorizationQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetVendorReturnAuthorizationQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetVendorReturnAuthorizationQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.user.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_VendorReturnAuthorization_Get_PendingReturn", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
