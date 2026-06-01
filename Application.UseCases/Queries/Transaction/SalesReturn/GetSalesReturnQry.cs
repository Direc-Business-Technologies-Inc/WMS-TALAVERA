using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SalesReturn;

public record GetSalesReturnQry(int DocEntry) : IRequest<SalesReturnDTO?>;

public class GetSalesReturnQryHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<GetSalesReturnQry, SalesReturnDTO?>
{
    public async Task<SalesReturnDTO?> Handle(GetSalesReturnQry request, CancellationToken cancellationToken)
    {
        SalesReturnHeaderSAPDTO? header = await salesReturnIntegration.GetSalesReturnHeaderAsync(request.DocEntry);

        if (header is null)
            return null;

        IEnumerable<SalesReturnLinesSAPDTO> lines = await salesReturnIntegration.GetSalesReturnLinesAsync(request.DocEntry);

        SalesReturnDTO dto = header.Adapt<SalesReturnDTO>();
        dto.DocumentLines = lines.Adapt<List<SalesReturnLineDTO>>();

        return dto;
    }
}
