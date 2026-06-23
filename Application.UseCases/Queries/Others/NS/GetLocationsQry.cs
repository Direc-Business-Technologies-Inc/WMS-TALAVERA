using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetLocationsQry() : IRequest<IEnumerable<LocationDTO>>;

public class GetLocationsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetLocationsQry, IEnumerable<LocationDTO>>
{
    public async Task<IEnumerable<LocationDTO>> Handle(
        GetLocationsQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<LocationDTO>("NS_Get_Locations");
        return Data.Adapt<IEnumerable<LocationDTO>>();
    }
}
