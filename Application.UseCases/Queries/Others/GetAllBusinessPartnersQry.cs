using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetAllBusinessPartnersQry(DataGridIntent Intent) : IRequest<(IEnumerable<BusinessPartnerDTO> Data, int Count)>;

public class GetAllBusinessPartnersQryHandler(
    IBusinessPartnerIntegration businessPartnerIntegration) 
    : IRequestHandler<GetAllBusinessPartnersQry, (IEnumerable<BusinessPartnerDTO> Data, int Count)>
{
    public async Task<(IEnumerable<BusinessPartnerDTO> Data, int Count)> Handle(GetAllBusinessPartnersQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<BusinessPartnerSAPDTO> Data, int Count) = await businessPartnerIntegration.GetAllAsync(request.Intent);

        return (Data.Adapt<IEnumerable<BusinessPartnerDTO>>(), Count);
    }
}
