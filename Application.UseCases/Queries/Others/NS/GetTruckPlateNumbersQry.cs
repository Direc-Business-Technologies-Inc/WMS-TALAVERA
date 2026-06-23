using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetTruckPlateNumbersQry() : IRequest<IEnumerable<TruckPlateNumberDTO>>;

public class GetTruckPlateNumbersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTruckPlateNumbersQry, IEnumerable<TruckPlateNumberDTO>>
{
    public async Task<IEnumerable<TruckPlateNumberDTO>> Handle(
        GetTruckPlateNumbersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<TruckPlateNumberDTO>("NS_Get_TruckPlateNumbers");
        return Data.Adapt<IEnumerable<TruckPlateNumberDTO>>();
    }
}