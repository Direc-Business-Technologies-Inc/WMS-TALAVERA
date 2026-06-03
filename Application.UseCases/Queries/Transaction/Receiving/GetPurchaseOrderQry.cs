using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using PurchaseOrderLineDTO = Application.DataTransferObjects.Transactions.Receiving.PurchaseOrderLineDTO;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrderQry(int DocEntry) : IRequest<PurchaseOrderDTO?>;

public class GetPurchaseOrderQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrderQry, PurchaseOrderDTO?>
{
    public async Task<PurchaseOrderDTO?> Handle(GetPurchaseOrderQry request, CancellationToken cancellationToken)
    {
        PurchaseOrderInfoNSDTO? headerResponse = await receivingIntegration.GetPurchaseOrderHeaderAsync(request.DocEntry);
        if (headerResponse is null) return null;

        IEnumerable<PurchaseOrderLineNSDTO> linesResponse = await receivingIntegration.GetPurchaseOrderLinesAsync(request.DocEntry);

        PurchaseOrderInfoDTO purchaseOrderDTO = headerResponse.Adapt<PurchaseOrderInfoDTO>();
        IEnumerable<PurchaseOrderLineDTO> purchaseOrderLinesDTO = linesResponse.Adapt<IEnumerable<PurchaseOrderLineDTO>>();

        PurchaseOrderDTO dto = new()
        {
            DocumentInfo = purchaseOrderDTO,
            DocumentLines = [.. purchaseOrderLinesDTO]
        };

        return dto;
    }
}
