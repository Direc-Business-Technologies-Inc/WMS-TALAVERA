using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others.NS;

public record GetHelpersQry() : IRequest<IEnumerable<HelperDTO>>;

public class GetHelpersQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetHelpersQry, IEnumerable<HelperDTO>>
{
    public async Task<IEnumerable<HelperDTO>> Handle(
        GetHelpersQry request,
        CancellationToken cancellationToken)
    {
        var Data = await netSuiteApiClientService.NetsuiteQuery<HelperDTO>("NS_Get_Helpers");
        return Data.Adapt<IEnumerable<HelperDTO>>();
    }
}