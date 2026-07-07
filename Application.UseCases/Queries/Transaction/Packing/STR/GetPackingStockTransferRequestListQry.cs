using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record GetPackingStockTransferRequestListQry(DataGridIntent Intent, int SubsidiaryId)
    : IRequest<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)>;

public class GetPackingStockTransferRequestListQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingStockTransferRequestListQry, (IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)>
{
    public Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> Handle(
        GetPackingStockTransferRequestListQry request,
        CancellationToken cancellationToken)
    {
        return integration.GetPackingStockTransferRequestList(request.Intent, request.SubsidiaryId);
    }
}
