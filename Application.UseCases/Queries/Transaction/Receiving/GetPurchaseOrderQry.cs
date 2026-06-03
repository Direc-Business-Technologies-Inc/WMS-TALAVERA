using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using ReceivingLineDTO = Application.DataTransferObjects.Transactions.Receiving.ReceivingLineDTO;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrderQry(int DocEntry) : IRequest<ReceivingDTO?>;

public class GetPurchaseOrderQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrderQry, ReceivingDTO?>
{
    public async Task<ReceivingDTO?> Handle(GetPurchaseOrderQry request, CancellationToken cancellationToken)
    {
        ReceivingInfoNSDTO? headerResponse = await receivingIntegration.GetPurchaseOrderHeaderAsync(request.DocEntry);
        if (headerResponse is null) return null;

        IEnumerable<ReceivingLineNSDTO> linesResponse = await receivingIntegration.GetPurchaseOrderLinesAsync(request.DocEntry);

        ReceivingInfoDTO purchaseOrderDTO = headerResponse.Adapt<ReceivingInfoDTO>();
        IEnumerable<ReceivingLineDTO> purchaseOrderLinesDTO = linesResponse.Adapt<IEnumerable<ReceivingLineDTO>>();

        ReceivingDTO dto = new()
        {
            DocumentInfo = purchaseOrderDTO,
            DocumentLines = [.. purchaseOrderLinesDTO]
        };

        return dto;
    }
}
