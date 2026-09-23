using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Commands.Transaction.TripTicket.NS;

public record UpdateTripTicketCmd(PostTripTicketDTO Data, List<ItemFulfillmentDTO> RemovedIF, List<ItemFulfillmentDTO> AddedIF) : ITransactionalRequest<ApiResult<bool>>;

public class UpdateTripTicketCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<UpdateTripTicketCmd, ApiResult<bool>>
{
    public async Task<ApiResult<bool>> Handle(UpdateTripTicketCmd request, CancellationToken cancellationToken)
    {
        try
        {
            bool result = await netSuiteApiClientService.UpdateTripTicket(request.Data, request.RemovedIF, request.AddedIF);

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