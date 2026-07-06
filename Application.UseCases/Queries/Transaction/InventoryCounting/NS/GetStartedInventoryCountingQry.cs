using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting.NS;

public record GetStartedInventoryCountingQry(RequestPerSubsidiaryDTO subsidiary) : IRequest<IEnumerable<OrdersDTO>>;

public class GetStartedInventoryCountingQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetStartedInventoryCountingQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetStartedInventoryCountingQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.subsidiary.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_InventoryCounting_Get_Started", parameters);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}