using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.SalesReturn;

public record GetSalesReturnDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<SalesReturnDataGridDTO> Data, int Count)>;

public class GetSalesReturnDataGridQryHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<GetSalesReturnDataGridQry, (IEnumerable<SalesReturnDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<SalesReturnDataGridDTO> Data, int Count)> Handle(GetSalesReturnDataGridQry request, CancellationToken cancellationToken)
    {
        (var Data, int Count) = await salesReturnIntegration.GetSalesReturnDataAsync(request.Intent);
        return (Data.Adapt<IEnumerable<SalesReturnDataGridDTO>>(), Count);
    }
}
