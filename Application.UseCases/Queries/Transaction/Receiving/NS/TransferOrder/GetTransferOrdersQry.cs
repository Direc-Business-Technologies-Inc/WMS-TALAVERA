using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;

public record GetTransferOrdersQry(RequestPerUserDTO user, int limit = 50, int offset = 0) : IRequest<IEnumerable<OrdersDTO>>;

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
