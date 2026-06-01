using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.SalesReturn;

public record GetSalesReturnRequestDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<SalesReturnRequestDataGridDTO> Data, int Count)>;

public class GetSalesReturnRequestDataGridQryHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<GetSalesReturnRequestDataGridQry, (IEnumerable<SalesReturnRequestDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<SalesReturnRequestDataGridDTO> Data, int Count)> Handle(GetSalesReturnRequestDataGridQry request, CancellationToken cancellationToken)
    {
        (var Data, int Count) = await salesReturnIntegration.GetSalesReturnRequestDataAsync(request.Intent);
        return (Data.Adapt<IEnumerable<SalesReturnRequestDataGridDTO>>(), Count);
    }
}
