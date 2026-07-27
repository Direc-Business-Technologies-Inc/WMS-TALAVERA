using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;

public record GetTransferOrderIFItemsQry(TransferOrderIFLineRequestDTO id) : IRequest<IEnumerable<TransferOrderLineDTO>>;

public class GetTransferOrderIFItemsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrderIFItemsQry, IEnumerable<TransferOrderLineDTO>>
{
    public async Task<IEnumerable<TransferOrderLineDTO>> Handle(
        GetTransferOrderIFItemsQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.id.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<TransferOrderLineDTO>("NS_TO_Get_Itemfulfillment_Items", parameters);
        return Data.Adapt<IEnumerable<TransferOrderLineDTO>>();
    }
}
