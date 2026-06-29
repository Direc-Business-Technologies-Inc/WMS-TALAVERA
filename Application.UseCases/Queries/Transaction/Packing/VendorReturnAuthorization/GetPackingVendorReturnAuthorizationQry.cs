using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.VendorReturnAuthorization;

public record GetPackingVendorReturnAuthorizationQry(string Ref) : IRequest<VendorReturnAuthorizationInfoDTO?>;

public class GetPackingVendorReturnAuthorizationQryHandler(IVendorReturnAuthorizationPackingIntegration integration)
    : IRequestHandler<GetPackingVendorReturnAuthorizationQry, VendorReturnAuthorizationInfoDTO?>
{
    public async Task<VendorReturnAuthorizationInfoDTO?> Handle(GetPackingVendorReturnAuthorizationQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPackingVendorReturnAuthorization(request.Ref);
    }
}
