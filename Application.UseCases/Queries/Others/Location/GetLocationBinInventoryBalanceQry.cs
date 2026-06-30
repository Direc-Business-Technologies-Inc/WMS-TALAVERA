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

public record GetLocationBinInventoryBalanceQry(DataGridIntent Intent, int binId) 
    : IRequest<(IEnumerable<InventoryBalanceDTO>, int)>;

public class GetLocationBinInventoryBalanceQryHandler(
    IInventoryIntegration integration
    ) : IRequestHandler<GetLocationBinInventoryBalanceQry, (IEnumerable<InventoryBalanceDTO>, int)>
{
    public async Task<(IEnumerable<InventoryBalanceDTO>, int)> Handle(GetLocationBinInventoryBalanceQry request, CancellationToken cancellationToken)
    {
        var intent = request.Intent.Adapt<DataGridIntent>();
        intent.Filters.Add(
            DataGridFilterUtilities.Equal(nameof(InventoryBalanceDTO.BinId), request.binId)
        );

        return await integration.GetInventoryBalance(intent);
    }
}
