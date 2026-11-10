using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record SubmitBulkFulfillmentCmd(SubmitBulkFulfillmentDTO Data, IReadOnlyList<int> AllowedSubsidiaries, Guid UserId = default)
    : IRequest<BulkFulfillmentResultDTO>;

public class SubmitBulkFulfillmentCmdHandler(ISupplierReturnIntegration integration, IBulkFulfillmentTrackingRepository tracking,
    ILogger<SubmitBulkFulfillmentCmdHandler> logger)
    : IRequestHandler<SubmitBulkFulfillmentCmd, BulkFulfillmentResultDTO>
{
    public async Task<BulkFulfillmentResultDTO> Handle(SubmitBulkFulfillmentCmd request, CancellationToken cancellationToken)
    {
        var ids = request.Data.ReturnIds?.Distinct().ToArray() ?? [];
        if (ids.Length < 2 || ids.Any(id => id <= 0))
            return BulkFulfillmentResultDTO.Reject("Select at least two unique RTS records with valid internal IDs.");
        if (request.UserId == Guid.Empty)
            return BulkFulfillmentResultDTO.Reject("Your user identity could not be established. Sign in again before submitting.");
        // Register process-local ownership before the write; callbacks may arrive before the receipt.
        var requestId = await tracking.BeginAsync(request.UserId, ids, cancellationToken);
        var result = await integration.SubmitBulkFulfillmentAsync(new(ids), request.AllowedSubsidiaries, cancellationToken);
        try
        {
            // Do not let browser cancellation discard a confirmed remote receipt.
            await tracking.CompleteSubmissionAsync(requestId, result, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not register bulk fulfillment receipt for local request {RequestId}; BF {BulkFulfillmentId}", requestId, result.ReferenceId);
            return result with { Message = result.Message + " Session notification tracking is unavailable. Check NetSuite using this reference before resubmitting." };
        }
        return result;
    }
}
