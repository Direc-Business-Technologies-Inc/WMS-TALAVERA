using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetCustomersQry(DataGridIntent Intent) : IRequest<(IEnumerable<BusinessPartnerDTO> Data, int Count)>;

public class GetCustomersQryHandler(
    IBusinessPartnerIntegration bpIntegration)
    : IRequestHandler<GetCustomersQry, (IEnumerable<BusinessPartnerDTO> Data, int Count)>
{
    public async Task<(IEnumerable<BusinessPartnerDTO> Data, int Count)> Handle(GetCustomersQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<BusinessPartnerSAPDTO> Data, int Count) = await bpIntegration.GetCustomersAsync(request.Intent);

        return (Data.Adapt<IEnumerable<BusinessPartnerDTO>>(), Count);
    }
}
