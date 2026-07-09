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

namespace Application.UseCases.Queries.Others.Location;

public record GetLocationInventoryBalanceQry(DataGridIntent Intent, int locationId)
    : IRequest<(IEnumerable<InventoryBalanceDTO>, int)>;

public class GetLocationInventoryBalanceQryHandler(
    IInventoryIntegration integration
    ) : IRequestHandler<GetLocationInventoryBalanceQry, (IEnumerable<InventoryBalanceDTO>, int)>
{
    public async Task<(IEnumerable<InventoryBalanceDTO>, int)> Handle(GetLocationInventoryBalanceQry request, CancellationToken cancellationToken)
    {
        var intent = request.Intent.Adapt<DataGridIntent>();
        intent.Filters.Add(
            DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.LocationId), request.locationId)
        );

        return await integration.GetInventoryBalance(intent);
    }
}
