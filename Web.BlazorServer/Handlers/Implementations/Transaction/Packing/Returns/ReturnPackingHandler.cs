using Application.UseCases.Queries.Transaction.Packing.Returns;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.Returns;

public class ReturnPackingHandler(ISender sender) : IReturnPackingHandler
{
    public async Task<(IEnumerable<ReturnsPackingDataGridVM> Data, int Count)> GetReturnsList(DataGridIntent intent, int subsidiaryId)
    {
        GetPackingReturnListQry query = new(intent, subsidiaryId);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<ReturnsPackingDataGridVM>>(), count);
    }

    public async Task<ReturnsInfoPackingVM?> GetPackingReturn(string reference)
    {
        GetPackingReturnQry query = new(reference);

        var dto = await sender.Send(query);
        if (dto is null) return null;

        var vm = dto.Adapt<ReturnsInfoPackingVM>();
        vm.SourceWarehouse = dto.Location;
        vm.DestinationWarehouse = dto.TransferLocation;

        return vm;
    }

    public async Task<(IEnumerable<ReturnsLinePackingVM> Data, int Count)> GetPackingReturnLines(string reference, DataGridIntent intent)
    {
        GetPackingReturnLinesQry query = new(reference, intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<ReturnsLinePackingVM>>(), count);
    }
}
