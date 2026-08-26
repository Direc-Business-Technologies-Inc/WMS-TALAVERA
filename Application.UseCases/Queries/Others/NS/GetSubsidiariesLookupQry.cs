using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetSubsidiariesQry() : IRequest<IEnumerable<SubsidiaryDTO>>;

public class GetSubsidiariesQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetSubsidiariesQry, IEnumerable<SubsidiaryDTO>>
{
    public async Task<IEnumerable<SubsidiaryDTO>> Handle(
        GetSubsidiariesQry request,
        CancellationToken cancellationToken)
    {
        var data = await netSuiteApiClientService.NetsuiteQuery<SubsidiaryDTO>("NS_Get_Subsidiaries");
        return data.Adapt<IEnumerable<SubsidiaryDTO>>();
    }
}