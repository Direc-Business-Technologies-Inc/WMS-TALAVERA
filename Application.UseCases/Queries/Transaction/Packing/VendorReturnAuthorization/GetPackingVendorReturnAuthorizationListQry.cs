using Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.VendorReturnAuthorization;

public record GetPackingVendorReturnAuthorizationListQry(DataGridIntent Intent, int SubsidiaryId)
    : IRequest<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)>;

public class GetPackingVendorReturnAuthorizationListQryHandler(IVendorReturnAuthorizationPackingIntegration integration)
    : IRequestHandler<GetPackingVendorReturnAuthorizationListQry, (IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<VendorReturnAuthorizationDataGridDTO> Data, int Count)> Handle(
        GetPackingVendorReturnAuthorizationListQry request,
        CancellationToken cancellationToken)
    {
        return integration.GetPackingVendorReturnAuthorizationsList(request.Intent, request.SubsidiaryId);
    }
}
