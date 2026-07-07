using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;

public record GetVendorReturnAuthorizationQry(RequestPerSubsidiaryDTO subsidiary) : IRequest<IEnumerable<OrdersDTO>>;

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
            ["subsidiaryid"] = request.subsidiary.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_VendorReturnAuthorization_Get_PendingReturn", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
