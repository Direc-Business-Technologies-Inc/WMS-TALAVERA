using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using System.Globalization;

namespace Application.UseCases.Queries.Transaction.TripTicket.NS;

public record GetPackedItemFulfillmentsQry(int SubsidiaryId) : IRequest<IEnumerable<OrdersDTO>>;

public class GetPackedItemFulfillmentsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetPackedItemFulfillmentsQry, IEnumerable<OrdersDTO>>
{
    public async Task<IEnumerable<OrdersDTO>> Handle(
        GetPackedItemFulfillmentsQry request,
        CancellationToken cancellationToken)
    {
        if (request.SubsidiaryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.SubsidiaryId));

        var Data = await netSuiteApiClientService.NetsuiteQuery<OrdersDTO>("NS_ItemFulfillment_Get_Packed",
            new() { ["subsidiaryid"] = request.SubsidiaryId.ToString(CultureInfo.InvariantCulture) },
            ct: cancellationToken);
        return Data.Adapt<IEnumerable<OrdersDTO>>();
    }
}
