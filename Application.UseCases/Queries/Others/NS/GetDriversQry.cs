using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetDriversQry() : IRequest<IEnumerable<DriverDTO>>;

public class GetDriversQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetDriversQry, IEnumerable<DriverDTO>>
{
    public async Task<IEnumerable<DriverDTO>> Handle(
        GetDriversQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<DriverDTO>("NS_Get_Drivers");
        return Data.Adapt<IEnumerable<DriverDTO>>();
    }
}