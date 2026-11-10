using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record GetPackingStockTransferRequestListQry(DataGridIntent Intent, int SubsidiaryId, bool IncludeReceived = false)
    : IRequest<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)>;

public class GetPackingStockTransferRequestListQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingStockTransferRequestListQry, (IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> Handle(
        GetPackingStockTransferRequestListQry request,
        CancellationToken cancellationToken)
    {
        var intent = request.Intent.Snapshot();
        if (cancellationToken.CanBeCanceled) intent.CancellationToken = cancellationToken;
        return integration.GetPackingStockTransferRequestList(intent, request.SubsidiaryId, request.IncludeReceived);
    }
}
