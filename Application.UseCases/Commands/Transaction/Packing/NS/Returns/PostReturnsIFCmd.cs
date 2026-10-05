using Application.DataTransferObjects.Transactions.Commons.NS;
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