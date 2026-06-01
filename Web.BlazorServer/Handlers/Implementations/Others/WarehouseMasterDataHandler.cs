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
    public async Task<(IEnumerable<WarehouseVM> Data, int Count)> GetWarehousesAsync(DataGridIntent intent)
    {
        GetWarehousesQry qry = new(intent);
        (IEnumerable<WarehouseDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<WarehouseVM>>(), Count);

    }
}
