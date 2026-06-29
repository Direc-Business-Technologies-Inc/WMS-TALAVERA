using Application.UseCases.Queries.Transaction.Packing.STR;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.STR;

public class StockTransferRequestPackingHandler(ISender sender) : IStockTransferRequestPackingHandler
{
    public async Task<StockTransferRequestInfoPackingVM?> GetPackingStockTransferRequest(string reference, bool includeLines = true)
    {
        GetPackingStockTransferRequestQry query = new(reference);
        var dto = await sender.Send(query);
        var vm = dto.Adapt<StockTransferRequestInfoPackingVM>();

        vm.Category = dto.TransferCategory;
        return vm;
    }

    public async Task<(IEnumerable<StockTransferRequestPackingDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent)
    {
        GetPackingStockTransferRequestListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestPackingDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<TransferOrderStatusPackingVM> data, int count)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        GetPackingTransferOrderStatusesQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<TransferOrderStatusPackingVM>>(), count);
    }
}
