using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetBarcodeDataQry(string Barcode, int? Location) : IRequest<BarcodeDTO?>;
public class GetBarcodeDataQryHandler(IReceivingIntegration integration)
    : IRequestHandler<GetBarcodeDataQry, BarcodeDTO?>
{
    public async Task<BarcodeDTO?> Handle(GetBarcodeDataQry request, CancellationToken cancellationToken)
    {
        return await integration.GetBarcodeData(request.Barcode, request.Location);
    }
}
