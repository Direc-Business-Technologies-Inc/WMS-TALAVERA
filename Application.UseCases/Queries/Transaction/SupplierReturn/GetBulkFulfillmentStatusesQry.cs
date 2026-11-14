using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetBulkFulfillmentStatusesQry(DataGridIntent Intent, IReadOnlyList<int> AllowedSubsidiaries)
    : IRequest<(IEnumerable<BulkFulfillmentStatusDTO> Data, int Count)>;

public class GetBulkFulfillmentStatusesQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetBulkFulfillmentStatusesQry, (IEnumerable<BulkFulfillmentStatusDTO> Data, int Count)>
{
    public Task<(IEnumerable<BulkFulfillmentStatusDTO> Data, int Count)> Handle(GetBulkFulfillmentStatusesQry request, CancellationToken ct) =>
        integration.GetBulkFulfillmentStatusesAsync(request.Intent, request.AllowedSubsidiaries, ct);
}
