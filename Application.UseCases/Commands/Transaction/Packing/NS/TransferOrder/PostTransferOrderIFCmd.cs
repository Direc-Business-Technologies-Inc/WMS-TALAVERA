using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;

public record PostTransferOrderIFCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostTransferOrderIFCmdHandler(
    INetSuiteApiClientService netSuiteApiClientService,
    IItemFulfillmentStatusResolver statusResolver) : IRequestHandler<PostTransferOrderIFCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostTransferOrderIFCmd request, CancellationToken cancellationToken)
    {
        if (!ItemFulfillmentRequestValidator.TryGetSourceTransactionId(
                request.Data,
                out var sourceTransactionId,
                out var validationError))
        {
            return ApiResult<bool>.Failed(validationError);
        }

        try
        {
            var status = await statusResolver.ResolveAsync(sourceTransactionId, cancellationToken);
            bool result = await netSuiteApiClientService.SaveTOItemFulfillment(request.Data, status);

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
