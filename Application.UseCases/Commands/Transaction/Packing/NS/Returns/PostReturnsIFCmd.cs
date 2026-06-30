using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.Packing.NS.Returns;

public record PostReturnsIFCmd(List<PostReturnsDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostReturnsIFCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostReturnsIFCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostReturnsIFCmd request, CancellationToken cancellationToken)
    {
        try
        {
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