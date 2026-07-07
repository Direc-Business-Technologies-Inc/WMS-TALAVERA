using Application.UseCases.Queries.Transaction.Packing.STR;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR;
using Web.BlazorServer.ViewModels.Transaction.Packing.STR;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.STR;

public class StockTransferRequestPackingHandler(ISender sender) : IStockTransferRequestPackingHandler
{
    public async Task<(IEnumerable<StockTransferRequestPackingDataGridVM> Data, int Count)> GetStockTransferRequestsList(DataGridIntent intent, int subsidiaryId)
    {
        GetPackingStockTransferRequestListQry query = new(intent, subsidiaryId);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestPackingDataGridVM>>(), count);
    }

    public async Task<StockTransferRequestInfoPackingVM?> GetPackingStockTransferRequest(string reference)
    {
        GetPackingStockTransferRequestQry query = new(reference);

        var dto = await sender.Send(query);
        if (dto is null) return null;

        var vm = dto.Adapt<StockTransferRequestInfoPackingVM>();
        vm.SourceWarehouse = dto.Location;
        vm.DestinationWarehouse = dto.TransferLocation;

        return vm;
    }

    public async Task<(IEnumerable<StockTransferRequestLinePackingVM> Data, int Count)> GetPackingStockTransferRequestLines(string reference, DataGridIntent intent)
    {
        GetPackingStockTransferRequestLinesQry query = new(reference, intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestLinePackingVM>>(), count);
    }
}
