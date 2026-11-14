using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.TripTicket.NS;

public record CancelTripTciketCmd(PostTripTicketDTO Data) : ITransactionalRequest<ApiResult<bool>>;

public class CancelTripTciketCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<CancelTripTciketCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(CancelTripTciketCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.CancelTripTicket(request.Data);

            if (!result)
            {
                return ApiResult<bool>.Failed("Failed to save scanned items to NetSuite.");
            }

            return ApiResult<bool>.Succeeded(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResult<bool> { StatusCode = 403, ErrorMessage = ex.Message };
        }
        catch (ArgumentException ex)
        {
            return ApiResult<bool>.Failed(ex.Message);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.ServerError(
                $"{ex.Message}"
            );
        }
    }
}
