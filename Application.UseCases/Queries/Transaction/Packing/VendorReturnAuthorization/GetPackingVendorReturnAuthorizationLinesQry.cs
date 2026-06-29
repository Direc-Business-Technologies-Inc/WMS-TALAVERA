using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.VendorReturnAuthorization;

public record GetPackingVendorReturnAuthorizationLinesQry(string Ref, DataGridIntent Intent)
    : IRequest<(IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)>;

public class GetPackingVendorReturnAuthorizationLinesQryHandler(IVendorReturnAuthorizationPackingIntegration integration)
    : IRequestHandler<GetPackingVendorReturnAuthorizationLinesQry, (IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)>
{
    public async Task<(IEnumerable<VendorReturnAuthorizationLineDTO> Data, int Count)> Handle(
        GetPackingVendorReturnAuthorizationLinesQry request,
        CancellationToken cancellationToken)
    {
        return await integration.GetPackingVendorReturnAuthorizationLines(request.Ref, request.Intent);
    }
}
