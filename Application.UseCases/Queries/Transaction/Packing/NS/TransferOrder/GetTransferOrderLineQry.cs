using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

public record GetTransferOrderLineQry(TransferOrderLineRequestDTO order) : IRequest<IEnumerable<TransferOrderLineDTO>>;

public class MGetTransferOrderLineQryHandler(
    INetSuiteApiClientService netSuiteApiClientService)
    : IRequestHandler<GetTransferOrderLineQry, IEnumerable<TransferOrderLineDTO>>
{
    // NetSuite caps a SuiteQL response at 1,000 rows. The script orders by
    // transactionline.id, so consecutive pages neither repeat nor skip a line.
    private const int PageSize = 1000;

    // A runaway loop would only happen against a broken server; 100 pages is
    // 100,000 rows, the neighbourhood of SuiteQL's own result ceiling.
    private const int MaxPages = 100;

    public async Task<IEnumerable<TransferOrderLineDTO>> Handle(
        GetTransferOrderLineQry request,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["tranid"] = request.order.OrderNumber,
            // The vendor gate knows exactly one vendor per scan, so the script
            // applies the restriction. Callers with no scope (web packing, the
            // vendor resolver) send the 0 sentinel and receive every line, which
            // is why no in-memory filter is needed or kept: filtering here would
            // transfer the whole document just to discard most of it on mobile.
            ["vendorId"] = request.order.VendorId is > 0 ? request.order.VendorId.Value.ToString() : "0"
        };

        var rows = new List<TransferOrderLineDTO>();

        // Latest reported total. Each page re-runs the query, so this tracks a
        // document that legitimately shrinks mid-read instead of holding a
        // stale first-page figure.
        int? expectedTotal = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var offset = page * PageSize;

            var response = await netSuiteApiClientService.NetsuiteQueryWithMetadata<TransferOrderLineDTO>(
                "NS_TO_x_Packing_Get_Items",
                parameters,
                PageSize,
                offset,
                cancellationToken);

            var batch = response?.items;
            if (batch is null || batch.Count == 0)
            {
                break;
            }

            rows.AddRange(batch);

            if (response!.totalResults > 0)
            {
                expectedTotal = response.totalResults;
            }

            // totalResults is authoritative and lets an exact multiple of the
            // page size stop on the last full page. NetSuite answers a request
            // at offset == totalResults with 404 INVALID_PARAMETER, never with
            // an empty page, so probing "one more page" would fail the read.
            if (response!.totalResults > 0 && rows.Count >= response.totalResults)
            {
                break;
            }

            if (batch.Count < PageSize || !response.hasMore)
            {
                break;
            }

            if (page == MaxPages - 1)
            {
                throw new InvalidOperationException(
                    $"Packing line read for transfer order '{request.order.OrderNumber}' exceeded " +
                    $"{MaxPages} pages ({MaxPages * PageSize:N0} lines). Refusing to return a partial list.");
            }
        }

        // A short or empty page is only a valid terminator when it accounts for
        // the total NetSuite reported. Otherwise the read would silently return
        // fewer lines than exist - the original defect in a different shape.
        if (expectedTotal is > 0 && rows.Count < expectedTotal)
        {
            throw new InvalidOperationException(
                $"Packing line read for transfer order '{request.order.OrderNumber}' returned " +
                $"{rows.Count:N0} of {expectedTotal:N0} lines. Refusing to return a partial list.");
        }

        return rows;
    }
}
