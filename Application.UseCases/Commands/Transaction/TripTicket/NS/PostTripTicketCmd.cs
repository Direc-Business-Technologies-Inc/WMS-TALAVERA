using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.TripTicket.NS;

public record PostTripTicketCmd(PostTripTicketDTO Data) : ITransactionalRequest<bool>;

public class PostTripTicketCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTripTicketCmd, bool>
{
    public async Task<bool> Handle(PostTripTicketCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveTripTicket(request.Data);

        return result;
    }
}