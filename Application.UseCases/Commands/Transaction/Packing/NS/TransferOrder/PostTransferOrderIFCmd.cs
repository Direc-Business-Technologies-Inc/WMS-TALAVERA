using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;

public record PostTransferOrderIFCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostTransferOrderIFCmdHandler(
    INetSuiteApiClientService netSuiteApiClientService,
    IStockTransferRequestPackingIntegration stockTransferRequestPackingIntegration,
    IStrAutoFulfillmentStore store,
    ILogger<PostTransferOrderIFCmdHandler> logger)
    : IRequestHandler<PostTransferOrderIFCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostTransferOrderIFCmd request, CancellationToken cancellationToken)
    {
        Guid? owner = null;
        var orderId = request.Data.FirstOrDefault()?.NetsuiteOrderInternalId ?? 0;
        var postingStarted = false;
        var releaseClaim = true;
        try
        {
            if (orderId <= 0 || request.Data.Any(x => x.NetsuiteOrderInternalId != orderId))
                return ApiResult<bool>.Failed("Exactly one transfer order is required. Nothing was saved.");
            owner = await store.TryAcquireAsync(orderId, cancellationToken);
            if (owner is null)
                return ApiResult<bool>.Failed("This STR is already being fulfilled or requires NetSuite reconciliation. Nothing was saved.");
            var firstLine = request.Data.FirstOrDefault();
            if (firstLine is not null)
            {
                var isTripTicketExempt = await stockTransferRequestPackingIntegration
                    .GetTripTicketExemption(firstLine.OrderNumber);

                // Fail closed. Guessing here posts a Packed ("B") ship status for an exempt order,
                // which strands goods that already shipped and pushes the order into the Trip
                // Ticket flow that exemption exists to bypass. Better to refuse the save and let
                // the operator retry than to write the wrong fulfillment state to NetSuite.
                if (isTripTicketExempt is null)
                {
                    logger.LogError(
                        "Trip-ticket exemption could not be resolved for transfer order {OrderNumber}. Save aborted to avoid posting an incorrect ship status.",
                        firstLine.OrderNumber);

                    return ApiResult<bool>.Failed(
                        $"Could not verify trip-ticket exemption for transfer order {firstLine.OrderNumber}. Nothing was saved. Please reload the order and try again.");
                }

                foreach (var line in request.Data)
                {
                    line.IsTripTicketExempt = isTripTicketExempt.Value;
                }
            }

            await store.MarkManualPostingAsync(orderId, owner.Value, cancellationToken);
            postingStarted = true;
            bool result = await netSuiteApiClientService.SaveTOItemFulfillment(request.Data);

            if (!result)
            {
                await store.MarkManualUncertainAsync(orderId, owner.Value, "Manual TO IF save returned false; reconcile NetSuite before retrying.", CancellationToken.None);
                postingStarted = false;
                return ApiResult<bool>.Failed("Failed to save scanned items to NetSuite.");
            }

            await store.CompleteManualAsync(orderId, owner.Value, CancellationToken.None);
            postingStarted = false;
            return ApiResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            if (postingStarted)
            {
                try { await store.MarkManualUncertainAsync(orderId, owner!.Value, ex.Message, CancellationToken.None); }
                catch (Exception recordError)
                {
                    logger.LogError(recordError, "Cannot record uncertain manual STR {OrderId}; retaining posting claim", orderId);
                    releaseClaim = false;
                }
            }
            return ApiResult<bool>.ServerError(
                $"{ex.Message}"
            );
        }
        finally
        {
            if (owner is not null && releaseClaim)
                await store.ReleaseAsync(orderId, owner.Value, CancellationToken.None);
        }
    }
}
