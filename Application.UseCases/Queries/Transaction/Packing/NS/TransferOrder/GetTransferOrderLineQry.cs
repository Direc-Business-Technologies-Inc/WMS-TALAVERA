using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

public record GetTransferOrderLineQry(TransferOrderLineRequestDTO order) : IRequest<IEnumerable<TransferOrderLineDTO>>;

public class MGetTransferOrderLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrderLineQry, IEnumerable<TransferOrderLineDTO>>
{
    public async Task<IEnumerable<TransferOrderLineDTO>> Handle(
        GetTransferOrderLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber
        };

        var Data = await netSuiteApiClientService.NetsuiteQuery<TransferOrderLineDTO>("NS_TO_x_Packing_Get_Items", parameters);

        // Optional vendor gate: the operator scans the vendor barcode printed on
        // the STR, and only that vendor's lines are returned. Filtered here in
        // memory because the script is shared with callers that must see every
        // line (the fulfillment save path posts zero-quantity entries for the
        // lines it was NOT asked to fulfill, and needs the full line set).
        var source = Data ?? Enumerable.Empty<TransferOrderLineDTO>();

        if (request.order.VendorId is > 0)
        {
            source = source.Where(line => line.VendorId == request.order.VendorId).ToList();
        }

        return source.Adapt<IEnumerable<TransferOrderLineDTO>>();
    }
}