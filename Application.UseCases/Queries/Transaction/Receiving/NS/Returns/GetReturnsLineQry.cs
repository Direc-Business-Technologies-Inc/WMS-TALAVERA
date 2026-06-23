using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving.NS.Returns;

public record GetReturnsLineQry(ReturnsLineRequestDTO order) : IRequest<IEnumerable<ReturnsLineDTO>>;

public class MGetReturnsLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetReturnsLineQry, IEnumerable<ReturnsLineDTO>>
{
    public async Task<IEnumerable<ReturnsLineDTO>> Handle(
        GetReturnsLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<ReturnsLineDTO>("NS_TransferOrder_x_Return_Get_Items", parameters);

        return Data.Adapt<IEnumerable<ReturnsLineDTO>>();
    }
}