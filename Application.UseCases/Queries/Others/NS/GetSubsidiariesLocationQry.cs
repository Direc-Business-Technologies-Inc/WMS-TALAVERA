using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetSubsidiariesLocationQry(RequestPerUserDTO subsidiary) : IRequest<IEnumerable<LocationDTO>>;

public class GetSubsidiariesLocationQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetSubsidiariesLocationQry, IEnumerable<LocationDTO>>
{
    public async Task<IEnumerable<LocationDTO>> Handle(
        GetSubsidiariesLocationQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["subsidiaryid"] = request.subsidiary.NetsuiteUserSubsidiaryInternalId.ToString()
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<LocationDTO>("NS_Subsidiary_Get_Locations", parameters);

        return Data.Adapt<IEnumerable<LocationDTO>>();
    }
}