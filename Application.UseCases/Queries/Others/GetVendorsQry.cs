using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetVendorsQry(DataGridIntent Intent) : IRequest<(IEnumerable<BusinessPartnerDTO> Data, int Count)>;

public class GetVendorsQryHandler(
    IBusinessPartnerIntegration bpIntegration) 
    : IRequestHandler<GetVendorsQry, (IEnumerable<BusinessPartnerDTO> Data, int Count)>
{
    public async Task<(IEnumerable<BusinessPartnerDTO> Data, int Count)> Handle(GetVendorsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<BusinessPartnerSAPDTO> Data, int Count) = await bpIntegration.GetVendorsAsync(request.Intent);

        return (Data.Adapt<IEnumerable<BusinessPartnerDTO>>(), Count);
    }
}
