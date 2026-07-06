using Application.DataTransferObjects.Others.Inventory;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.InventoryData;

public record GetInventoryStatusesQry(DataGridIntent Intent)
    : IRequest<(IEnumerable<InventoryStatusDTO>, int)>;

public class GetInventoryStatusesQryHandler(IInventoryIntegration integration)
    : IRequestHandler<GetInventoryStatusesQry, (IEnumerable<InventoryStatusDTO>, int)>
{
    public Task<(IEnumerable<InventoryStatusDTO>, int)> Handle(GetInventoryStatusesQry request, CancellationToken cancellationToken)
    {
        return integration.GetInventoryStatus(request.Intent);
    }
}
