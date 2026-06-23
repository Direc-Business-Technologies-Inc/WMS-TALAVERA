using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others;

public record GetItemUnitsQry(int itemId, DataGridIntent Intent) : IRequest<(IEnumerable<ItemUnitDTO> Data, int Count)>;

public class GetItemUnitsQryHandler(IItemsIntegration integration)
    : IRequestHandler<GetItemUnitsQry, (IEnumerable<ItemUnitDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ItemUnitDTO> Data, int Count)> Handle(GetItemUnitsQry request, CancellationToken cancellationToken)
    {
        return await integration.GetItemUnits(request.itemId, request.Intent);
    }
}