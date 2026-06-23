using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;

public record GetVendorReturnAuthorizationLineQry(VendorReturnAuthorizationLineRequestDTO order) : IRequest<IEnumerable<VendorReturnAuthorizationLineDTO>>;

public class GetVendorReturnAuthorizationLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetVendorReturnAuthorizationLineQry, IEnumerable<VendorReturnAuthorizationLineDTO>>
{
    public async Task<IEnumerable<VendorReturnAuthorizationLineDTO>> Handle(
        GetVendorReturnAuthorizationLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<VendorReturnAuthorizationLineDTO>("NS_VendorReturnAuthorization_Get_Items", parameters);

        return Data.Adapt<IEnumerable<VendorReturnAuthorizationLineDTO>>();
    }
}