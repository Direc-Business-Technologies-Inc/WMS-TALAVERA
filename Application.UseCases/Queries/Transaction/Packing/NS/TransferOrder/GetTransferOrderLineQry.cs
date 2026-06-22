using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

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

        var Data = await netSuiteApiClientService.NetsuiteQuery<TransferOrderLineDTO>("NS_TO_x_Packing_Get_Items", parameters);

        return Data.Adapt<IEnumerable<TransferOrderLineDTO>>();
    }
}