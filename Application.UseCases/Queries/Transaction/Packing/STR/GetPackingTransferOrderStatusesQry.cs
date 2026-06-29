using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.STR;

public record class GetPackingTransferOrderStatusesQry(DataGridIntent Intent) : IRequest<(IEnumerable<TransferOrderStatusPacking>, int count)>;

public class GetPackingTransferOrderStatusesQryHandler(IStockTransferRequestPackingIntegration integration)
    : IRequestHandler<GetPackingTransferOrderStatusesQry, (IEnumerable<TransferOrderStatusPacking>, int count)>
{
    public async Task<(IEnumerable<TransferOrderStatusPacking>, int count)> Handle(GetPackingTransferOrderStatusesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPackingTransferOrderStatuses(request.Intent);
    }
}
