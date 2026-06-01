using Application.DataTransferObjects.Transactions.Delivery;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Delivery;

public record PostDeliveryCmd(DeliveryDTO Data) : ITransactionalRequest<bool>;

public class PostDeliveryCmdHandler(IDeliveryIntegration deliveryIntegration) : IRequestHandler<PostDeliveryCmd, bool>
{
    public async Task<bool> Handle(PostDeliveryCmd request, CancellationToken cancellationToken)
    {
        bool result = await deliveryIntegration.PostDeliveryDocument(request.Data);

        return result;
    }
}
