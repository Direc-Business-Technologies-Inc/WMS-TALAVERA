using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Delivery;

public record GetDeliveryDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<DeliveryDataGridDTO> Data, int Count)>;

public class GetDeliveryDataGridQryHandler(
    IDeliveryIntegration deliveryIntegration) 
    : IRequestHandler<GetDeliveryDataGridQry, (IEnumerable<DeliveryDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<DeliveryDataGridDTO> Data, int Count)> Handle(GetDeliveryDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<DeliveryDataGridSAPDTO> Data, int Count) = await deliveryIntegration.GetDeliveryDocumentsAsync(request.Intent);

        return (Data.Adapt<IEnumerable<DeliveryDataGridDTO>>(), Count);
    }
}