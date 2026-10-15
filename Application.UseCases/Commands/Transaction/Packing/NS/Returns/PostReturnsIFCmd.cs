using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Packing.NS.Returns;

public record PostReturnsIFCmd(List<PostReturnsDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostReturnsIFCmdHandler(
    INetSuiteApiClientService netSuiteApiClientService,
    IReturnPackingIntegration returnPackingIntegration,
    ILogger<PostReturnsIFCmdHandler> logger)
    : IRequestHandler<PostReturnsIFCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostReturnsIFCmd request, CancellationToken cancellationToken)
    {
        try
        {
            var firstLine = request.Data.FirstOrDefault();
            if (firstLine is not null)
            {
                var isTripTicketExempt = await returnPackingIntegration
                    .GetTripTicketExemption(firstLine.OrderNumber);

                // Fail closed. Guessing here posts a Packed ("B") ship status for an exempt order,
                // which strands goods that already shipped and pushes the order into the Trip
                // Ticket flow that exemption exists to bypass. Better to refuse the save and let
                // the operator retry than to write the wrong fulfillment state to NetSuite.
                if (isTripTicketExempt is null)
                {
                    logger.LogError(
                        "Trip-ticket exemption could not be resolved for return {OrderNumber}. Save aborted to avoid posting an incorrect ship status.",
                        firstLine.OrderNumber);

                    return ApiResult<bool>.Failed(
                        $"Could not verify trip-ticket exemption for return {firstLine.OrderNumber}. Nothing was saved. Please reload the order and try again.");
                }

                foreach (var line in request.Data)
                {
                    line.IsTripTicketExempt = isTripTicketExempt.Value;
                }
            }

            // Nothing scanned means there is no fulfillment to post. Posting anyway sends a
            // zero-quantity body that NetSuite rejects with an opaque 400.
            if (!request.Data.Any(line => line.ScannedQuantity > 0))
            {
                return ApiResult<bool>.Failed(
                    "Please enter at least one quantity to fulfill. Nothing was saved.");
            }

            // Fail closed on an unrecognised transfer category. This is a header-level field, so an
            // unmapped value (0, stale mapping) would otherwise post every line against whatever
            // status the payload builder inferred, silently mis-stating inventory.
            var badCategory = request.Data.FirstOrDefault(line =>
                !line.IsBad && !line.IsMissing && !InventoryStatusMap.IsKnownTransferCategory(line.TransferCategory));

            if (badCategory is not null)
            {
                logger.LogError(
                    "Return {OrderNumber} line {LineSequenceNumber} has unrecognised transfer category {TransferCategory}. Save aborted.",
                    badCategory.OrderNumber,
                    badCategory.LineSequenceNumber,
                    badCategory.TransferCategory);

                return ApiResult<bool>.Failed(
                    $"Return {badCategory.OrderNumber} line {badCategory.LineSequenceNumber} has an unrecognised " +
                    $"transfer category ({badCategory.TransferCategory}). Nothing was saved. " +
                    "Please reload the order and try again.");
            }

            bool result = await netSuiteApiClientService.SaveReturnsItemFulfillment(request.Data);

            if (!result)
            {
                return ApiResult<bool>.Failed("Failed to save scanned items to NetSuite.");
            }

            return ApiResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.ServerError(
                $"{ex.Message}"
            );
        }
    }
}