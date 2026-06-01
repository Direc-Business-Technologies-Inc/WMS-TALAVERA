using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPuchaseDeliveryNoteQry(int DocEntry) : IRequest<PurchaseDeliveryNoteDTO?>;

public class GetPuchaseDeliveryNoteQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPuchaseDeliveryNoteQry, PurchaseDeliveryNoteDTO?>
{
    public async Task<PurchaseDeliveryNoteDTO?> Handle(GetPuchaseDeliveryNoteQry request, CancellationToken cancellationToken)
    {
        PurchaseDeliveryNoteHeaderSAPDTO? headerResponse = await receivingIntegration.GetPurchaseDeliveryNoteHeaderAsync(request.DocEntry);
        if(headerResponse is null)
            return null;

        IEnumerable<PurchaseDeliveryNoteLineSAPDTO> linesResponse = await receivingIntegration.GetPurchaseDeliveryNoteLinesAsync(request.DocEntry);

        PurchaseDeliveryNoteDTO purchaseDeliveryNoteDTO = headerResponse.Adapt<PurchaseDeliveryNoteDTO>();
        IEnumerable<PurchaseDeliveryNoteLineDTO> purchaseDeliveryNoteLineDTO = linesResponse.Adapt<IEnumerable<PurchaseDeliveryNoteLineDTO>>();

        purchaseDeliveryNoteDTO.DocumentLines = [.. purchaseDeliveryNoteLineDTO];

        return purchaseDeliveryNoteDTO;
    }
}
