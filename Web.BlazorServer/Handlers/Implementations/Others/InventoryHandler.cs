using Application.UseCases.Queries.Others.InventoryData;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class InventoryHandler(ISender sender) : IInventoryHandler
{
    public async Task<(IEnumerable<InventoryBalanceVM>, int)> GetInventoryBalanceAsync(DataGridIntent intent, int? locationId = null, int? itemId = null, int? binId = null, int? statusId = null)
    {
        GetInventoryBalanceQry qry = new(
            intent,
            locationId: locationId,
            itemId: itemId,
            binId: binId,
            statusId: statusId
        );

        (var data, int count) = await sender.Send(qry);

        return (data.Adapt<IEnumerable<InventoryBalanceVM>>(), count);
    }

    public async Task<IEnumerable<InventoryDetailVM>> GetInventoryDetails(int documentId, int lineId)
    {
        GetInventoryDetailsQry qry = new(documentId, lineId);
        var details = await sender.Send(qry);
        return details.Adapt<IEnumerable<InventoryDetailVM>>();
    }

    public async Task<(IEnumerable<InventoryStatusVM>, int)> GetInventoryStatusAsync(DataGridIntent intent)
    {
        GetInventoryStatusesQry qry = new(intent);
        (var data, int count) = await sender.Send(qry);
        return (data.Adapt<IEnumerable<InventoryStatusVM>>(), count);
    }
}
