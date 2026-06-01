using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Delivery;

public record GetDeliveryMeansQry() : IRequest<IEnumerable<DeliveryMeansDTO>>;

public class GetDeliveryMeansQryHandler(
    IDeliveryIntegration deliveryIntegration)
    : IRequestHandler<GetDeliveryMeansQry, IEnumerable<DeliveryMeansDTO>>
{
    public async Task<IEnumerable<DeliveryMeansDTO>> Handle(GetDeliveryMeansQry request, CancellationToken cancellationToken)
    {
        IEnumerable<DeliveryMeansSAPDTO> response = await deliveryIntegration.GetDeliveryMeansAsync();

        return response.Adapt<IEnumerable<DeliveryMeansDTO>>();
    }
}