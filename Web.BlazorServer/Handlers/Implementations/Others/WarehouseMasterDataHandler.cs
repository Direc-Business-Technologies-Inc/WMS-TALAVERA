using Application.DataTransferObjects.Others;
using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class WarehouseMasterDataHandler(
    ISender Sender)
    : IWarehouseMasterDataHandler
{
    public async Task<(IEnumerable<WarehouseVM> Data, int Count)> GetWarehousesAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetWarehousesQry qry = new(intent);
        (IEnumerable<WarehouseDTO> Data, int Count) = await Sender.Send(qry, ct);

        return (Data.Adapt<IEnumerable<WarehouseVM>>(), Count);

    }
}
