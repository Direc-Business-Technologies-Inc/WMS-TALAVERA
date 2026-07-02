using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;
public record GetBinsPerLocationQry(BinLocationRequestDTO locations) : IRequest<IEnumerable<BinPerLocationDTO>>;

public class GetBinsPerLocationQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetBinsPerLocationQry, IEnumerable<BinPerLocationDTO>>
{
    public async Task<IEnumerable<BinPerLocationDTO>> Handle(
        GetBinsPerLocationQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["locations"] = string.Join(",", request.locations.Location)
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<BinPerLocationDTO>("NS_Bin_Get_Locations", parameters);

        return Data.Adapt<IEnumerable<BinPerLocationDTO>>();
    }
}