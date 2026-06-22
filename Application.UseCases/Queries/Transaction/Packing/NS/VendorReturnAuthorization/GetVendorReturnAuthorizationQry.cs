using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;

public record GetVendorReturnAuthorizationQry() : IRequest<IEnumerable<OrdersDTO>>;

public class GetVendorReturnAuthorizationQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetVendorReturnAuthorizationQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetVendorReturnAuthorizationQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_VendorReturnAuthorization_Get_PendingReturn");
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
