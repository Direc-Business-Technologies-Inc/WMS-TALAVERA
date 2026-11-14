using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetBulkFulfillmentReturnsQry(int BulkId, IReadOnlyList<int> AllowedSubsidiaries)
    : IRequest<IReadOnlyList<SupplierReturnDataGridDTO>>;

public class GetBulkFulfillmentReturnsQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetBulkFulfillmentReturnsQry, IReadOnlyList<SupplierReturnDataGridDTO>>
{
    public Task<IReadOnlyList<SupplierReturnDataGridDTO>> Handle(GetBulkFulfillmentReturnsQry request, CancellationToken ct) =>
        integration.GetBulkFulfillmentReturnsAsync(request.BulkId, request.AllowedSubsidiaries, ct);
}
