using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.TripTicket.NS;

public record PostTripTicketCmd(PostTripTicketDTO Data) : ITransactionalRequest<ApiResult<bool>>;

public class PostTripTicketCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTripTicketCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(PostTripTicketCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.SaveTripTicket(request.Data);

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