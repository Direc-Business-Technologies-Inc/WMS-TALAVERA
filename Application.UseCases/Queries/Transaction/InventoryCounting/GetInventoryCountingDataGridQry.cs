using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.InventoryCounting;

public record GetInventoryCountingDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<InventoryCountingDataGridDTO> Data, int Count)>;

public class GetInventoryCountingDataGridQryHandler(
    IInventoryCountingReadRepo readRepo)
    : IRequestHandler<GetInventoryCountingDataGridQry, (IEnumerable<InventoryCountingDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<InventoryCountingDataGridDTO> Data, int Count)> Handle(GetInventoryCountingDataGridQry request, CancellationToken cancellationToken)
    {
        return await readRepo.GetInventoryCountingDataGrid(request.Intent);
    }
}
