using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Delivery;

public record GetDeliveryQry(int DocEntry) : IRequest<DeliveryDTO?>;

public class GetDeliveryQryHandler(
    IDeliveryIntegration deliveryIntegration) 
    : IRequestHandler<GetDeliveryQry, DeliveryDTO?>
{
    public async Task<DeliveryDTO?> Handle(GetDeliveryQry request, CancellationToken cancellationToken)
    {
        DeliveryHeaderSAPDTO? doc = await deliveryIntegration.GetDeliveryDocumentHeaderAsync(request.DocEntry);
        
        if(doc is null)
            return null;

        IEnumerable<DeliveryLineSAPDTO> lines = await deliveryIntegration.GetDeliveryDocumentLinesAsync(request.DocEntry);

        DeliveryDTO dto = doc.Adapt<DeliveryDTO>();
        dto.DocumentLines = lines.Adapt<IEnumerable<DeliveryLineDTO>>();

        return dto;
    }
}