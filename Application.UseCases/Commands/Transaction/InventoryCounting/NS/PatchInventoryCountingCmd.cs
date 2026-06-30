using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.InventoryCounting.NS;

public record PatchInventoryCountingCmd(List<PatchInventoryCountingDTO> Data) : ITransactionalRequest<ApiResult<bool>>;

public class PatchInventoryCountingCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PatchInventoryCountingCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PatchInventoryCountingCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.PatchInventoryCounting(request.Data);

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