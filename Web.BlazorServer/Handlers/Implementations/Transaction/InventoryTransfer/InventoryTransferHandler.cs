using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Commands.Transaction.InventoryTransfer;
using Application.UseCases.Queries.Transaction.InventoryTransfer;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransfer;

public class InventoryTransferHandler(ISender Sender) : IInventoryTransferHandler
{
    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferRequestsQry qry = new(intent);
        (IEnumerable<InventoryTransferDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), Count);
    }

    public async Task<InventoryTransferRequestVM> GetInventoryTransferRequestAsync(int id)
    {
        GetInventoryTransferRequestQry qry = new(id);
        InventoryTransferRequestDTO? response = await Sender.Send(qry);

        return response.Adapt<InventoryTransferRequestVM>();
    }
    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetPostedInventoryTransferDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferPostedRequestsQry qry = new(intent);
        (IEnumerable<InventoryTransferDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), Count);
    }
    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetPendingInventoryTransferDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferPendingRequestsQry qry = new(intent);
        (IEnumerable<InventoryTransferDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), Count);
    }

    public async Task<InventoryTransferCVUVM> GetPostedInventoryTransferRequestAsync(int id)
    {
        GetInventoryTransferPostedRequestQry qry = new(id);
        InventoryTransferDTO? response = await Sender.Send(qry);

        return response.Adapt<InventoryTransferCVUVM>();
    }

    public async Task<InventoryTransferCVUVM> GetPendingInventoryTransferRequestAsync(int id)
    {
        GetInventoryTransferPendingRequestQry qry = new(id);
        InventoryTransferDTO? response = await Sender.Send(qry);

        return response.Adapt<InventoryTransferCVUVM>();
    }

    public async Task<int> PostInventoryTransferRequestAsync(InventoryTransferRequestVM data)
    {
        PostInventoryTransferRequestCmd cmd = new(data.Adapt<InventoryTransferRequestDTO>());
        int response = await Sender.Send(cmd);

        return response;
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetRejectedInventoryTransferDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferRejectedRequestsQry qry = new(intent);
        (IEnumerable<InventoryTransferDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), Count);
    }

    public async Task<InventoryTransferCVUVM> GetRejectedInventoryTransferRequestAsync(int id)
    {
        GetInventoryTransferRejectedRequestQry qry = new(id);
        InventoryTransferDTO? response = await Sender.Send(qry);

        return response.Adapt<InventoryTransferCVUVM>();
    }
}
