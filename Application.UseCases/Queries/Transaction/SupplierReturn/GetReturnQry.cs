using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetReturnQry(string Ref) : IRequest<SupplierReturnDTO?>;

public class GetReturnQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetReturnQry, SupplierReturnDTO?>
{
    public async Task<SupplierReturnDTO?> Handle(GetReturnQry request, CancellationToken cancellationToken)
    {
        var header = await integration.GetReturnAsync(request.Ref);
        if (header is null) return null;

        var lines = await integration.GetReturnLinesAsync(request.Ref);
        header.Lines = [.. lines];

        return header;
    }
}
