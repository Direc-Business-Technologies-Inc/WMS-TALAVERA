using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Domain.Entities.Enums.Transaction.SalesReturn;
using MediatR;

namespace Application.UseCases.Commands.Transaction.SalesReturn;

public record PostSalesReturnCmd(SalesReturnDTO Data, SalesReturnPostingSource Source) : IRequest<bool>;

public class PostSalesReturnCmdHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<PostSalesReturnCmd, bool>
{
    public async Task<bool> Handle(PostSalesReturnCmd request, CancellationToken cancellationToken)
    {
        bool result = false;

        if (request.Source == SalesReturnPostingSource.Standalone)
            result = await salesReturnIntegration.PostSalesReturnAsync(request.Data);
        else if (request.Source == SalesReturnPostingSource.Delivery)
            result = await salesReturnIntegration.PostSalesReturnFromDeliveryAsync(request.Data);
        else if (request.Source == SalesReturnPostingSource.Request)
            result = await salesReturnIntegration.PostSalesReturnFromRequestAsync(request.Data);

        return result;
    }
}
