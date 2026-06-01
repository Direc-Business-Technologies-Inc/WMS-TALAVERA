using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Delivery;

public record GetSalesOrderQry(int DocEntry) : IRequest<SalesOrderDTO?>;

public class GetSalesOrderQryHandler(
    IDeliveryIntegration deliveryIntegration)
    : IRequestHandler<GetSalesOrderQry, SalesOrderDTO?>
{
    public async Task<SalesOrderDTO?> Handle(GetSalesOrderQry request, CancellationToken cancellationToken)
    {
        SalesOrderHeaderSAPDTO? doc = await deliveryIntegration.GetSalesOrderDocumentHeaderAsync(request.DocEntry);

        if (doc is null)
            return null;

        IEnumerable<SalesOrderLineSAPDTO> lines = await deliveryIntegration.GetSalesOrderDocumentLinesAsync(request.DocEntry);

        SalesOrderDTO dto = doc.Adapt<SalesOrderDTO>();
        dto.DocumentLines = lines.Adapt<IEnumerable<SalesOrderLineDTO>>();

        return dto;
    }
}