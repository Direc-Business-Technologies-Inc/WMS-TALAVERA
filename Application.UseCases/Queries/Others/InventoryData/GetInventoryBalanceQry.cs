using Application.DataTransferObjects.Others.Inventory;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.InventoryData;

public record GetInventoryBalanceQry(DataGridIntent Intent, int? locationId = null, int? statusId = null, int? itemId = null, int? binId = null) 
    : IRequest<(IEnumerable<InventoryBalanceDTO>, int)>;
public class GetInventoryBalanceQryHandler(IInventoryIntegration integration)
    : IRequestHandler<GetInventoryBalanceQry, (IEnumerable<InventoryBalanceDTO>, int)>
{
    public Task<(IEnumerable<InventoryBalanceDTO>, int)> Handle(GetInventoryBalanceQry request, CancellationToken cancellationToken)
    {
        var newIntent = request.Intent.Adapt<DataGridIntent>();
        if (request.locationId is not null)
            newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.LocationId), request.locationId));
        if (request.statusId is not null)
            newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.StatusId), request.statusId));
        if (request.itemId is not null)
            newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.ItemId), request.itemId));
        if (request.binId is not null)
            newIntent.Filters.Add(DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.BinId), request.binId));

        return integration.GetInventoryBalance(newIntent);
    }
}
