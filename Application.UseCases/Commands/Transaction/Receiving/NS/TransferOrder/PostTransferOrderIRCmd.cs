using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;

public record PostTransferOrderIRCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostTransferOrderIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTransferOrderIRCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostTransferOrderIRCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.SaveTOItemReceipt(request.Data);

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