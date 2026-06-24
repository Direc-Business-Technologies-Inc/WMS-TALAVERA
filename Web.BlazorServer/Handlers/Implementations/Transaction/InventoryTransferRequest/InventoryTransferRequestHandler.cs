using Application.UseCases.Queries.Transaction.InventoryTransferRequests;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestHandler(ISender sender) : IInventoryTransferRequestHandler
{
    public async Task<InventoryTransferRequestVM?> GetInventoryTransferRequestAsync(string Ref)
    {
        GetInventoryTransferRequestQry query = new(Ref);

        var response = await sender.Send(query);
        if (response is null) return null;

        return response.Adapt<InventoryTransferRequestVM>();
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferRequestDataGridQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), count);
    }
}
