using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record SubmitBulkFulfillmentCmd(SubmitBulkFulfillmentDTO Data, IReadOnlyList<int> AllowedSubsidiaries)
    : IRequest<BulkFulfillmentResultDTO>;

public class SubmitBulkFulfillmentCmdHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<SubmitBulkFulfillmentCmd, BulkFulfillmentResultDTO>
{
    public async Task<BulkFulfillmentResultDTO> Handle(SubmitBulkFulfillmentCmd request, CancellationToken cancellationToken)
    {
        var ids = request.Data.ReturnIds?.Distinct().ToArray() ?? [];
        if (ids.Length < 2 || ids.Any(id => id <= 0))
            return BulkFulfillmentResultDTO.Reject("Select at least two unique RTS records with valid internal IDs.");
        return await integration.SubmitBulkFulfillmentAsync(new(ids, request.Data.Memo), request.AllowedSubsidiaries, cancellationToken);
    }
}
