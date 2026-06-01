using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SalesReturn;

public record GetSalesReturnRequestQry(int DocEntry) : IRequest<SalesReturnRequestDTO?>;

public class GetSalesReturnRequestQryHandler(
    ISalesReturnIntegration salesReturnIntegration)
    : IRequestHandler<GetSalesReturnRequestQry, SalesReturnRequestDTO?>
{
    public async Task<SalesReturnRequestDTO?> Handle(GetSalesReturnRequestQry request, CancellationToken cancellationToken)
    {
        SalesReturnRequestHeaderSAPDTO? header = await salesReturnIntegration.GetSalesReturnRequestHeaderAsync(request.DocEntry);

        if (header is null)
            return null;

        IEnumerable<SalesReturnRequestLinesSAPDTO> lines = await salesReturnIntegration.GetSalesReturnRequestLinesAsync(request.DocEntry);

        SalesReturnRequestDTO dto = header.Adapt<SalesReturnRequestDTO>();
        dto.DocumentLines = lines.Adapt<List<SalesReturnRequestLineDTO>>();

        return dto;
    }
}
