using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrderQry(int DocEntry) : IRequest<PurchaseOrderDTO?>;

public class GetPurchaseOrderQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrderQry, PurchaseOrderDTO?>
{
    public async Task<PurchaseOrderDTO?> Handle(GetPurchaseOrderQry request, CancellationToken cancellationToken)
    {
        PurchaseOrderHeaderSAPDTO? headerResponse = await receivingIntegration.GetPurchaseOrderHeaderAsync(request.DocEntry);
        if (headerResponse is null)
            return null;
        IEnumerable<PurchaseOrderLineSAPDTO> linesResponse = await receivingIntegration.GetPurchaseOrderLinesAsync(request.DocEntry);

        PurchaseOrderDTO purchaseOrderDTO = headerResponse.Adapt<PurchaseOrderDTO>();
        IEnumerable<PurchaseOrderLineDTO> purchaseOrderLinesDTO = linesResponse.Adapt<IEnumerable<PurchaseOrderLineDTO>>();

        purchaseOrderDTO.DocumentLines = [.. purchaseOrderLinesDTO];

        return purchaseOrderDTO;
    }
}
