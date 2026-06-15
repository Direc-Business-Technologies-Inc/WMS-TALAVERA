using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;

public record GetTransferOrderLineQry(TransferOrderLineRequestDTO order) : IRequest<IEnumerable<TransferOrderLineDTO>>;

public class MGetTransferOrderLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrderLineQry, IEnumerable<TransferOrderLineDTO>>
{
    public async Task<IEnumerable<TransferOrderLineDTO>> Handle(
        GetTransferOrderLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<TransferOrderLineDTO>("NS_TransferOrder_Get_Items", parameters);

        return Data.Adapt<IEnumerable<TransferOrderLineDTO>>();
    }
}