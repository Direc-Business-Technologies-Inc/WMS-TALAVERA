using Application.UseCases.Queries.Transaction.InventoryTransferRequests;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestHandler(ISender sender) : IInventoryTransferRequestHandler
{
    public Task<InventoryTransferRequestVM?> GetInventoryTransferRequestAsync(string Ref)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferRequestDataGridQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), count);
    }
}
