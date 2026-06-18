using Application.UseCases.Queries.Transaction.InventoryAdjustment;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryAdjustment;

public class InventoryAdjustmentHandler(ISender sender) : IInventoryAdjustmentHandler
{
    public async Task<InventoryAdjustmentVM?> GetInventoryAdjustmentAsync(string id)
    {
        GetInventoryAdjustmentQry query = new(id);

        var dto = await sender.Send(query);

        return dto?.Adapt<InventoryAdjustmentVM>();
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetInventoryAdjustmentsDataGridAsync(DataGridIntent intent)
    {
        GetInventoryAdjustmentsQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentDataGridVM>>(), count);
    }
}
