using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.Returns;

public record PostReturnsIRCmd(SaveReturnRequestDTO Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostReturnsIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostReturnsIRCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostReturnsIRCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.SaveReturnsItemReceipt(request.Data.PostReturn, request.Data.TONetsuiteOrderInternalId, request.Data.UserId);

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