using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;

public record PostPurchaseOrderIRCmd(SavePurchaseOrderRequestDTO Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostPurchaseOrderIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostPurchaseOrderIRCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostPurchaseOrderIRCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.SavePOItemReceipt(request.Data.PostPurchaseOrders, request.Data.UserId);

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