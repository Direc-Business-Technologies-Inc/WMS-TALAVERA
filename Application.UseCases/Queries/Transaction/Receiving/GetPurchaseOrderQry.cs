using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using ReceivingLineDTO = Application.DataTransferObjects.Transactions.Receiving.ReceivingLineDTO;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseOrderQry(string DocEntry) : IRequest<PurchaseOrderDTO?>;

public class GetPurchaseOrderQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseOrderQry, PurchaseOrderDTO?>
{
    public async Task<PurchaseOrderDTO?> Handle(GetPurchaseOrderQry request, CancellationToken cancellationToken)
    {
        PurchaseOrderDTO? headerResponse = await receivingIntegration.GetPurchaseOrderHeaderAsync(request.DocEntry);
        if (headerResponse is null) return null;

        IEnumerable<PurchaseOrderLineDTO> linesResponse = await receivingIntegration.GetPurchaseOrderLinesAsync(request.DocEntry);


        headerResponse.Lines = [.. linesResponse];
        return headerResponse;
    }
}
