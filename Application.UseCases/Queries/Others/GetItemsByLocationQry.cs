using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others;

public record GetItemsByLocationQry(DataGridIntent Intent, int locationId) : IRequest<(IEnumerable<ItemsDTO> data, int count)>;

public class GetItemsQryByLocationHandler(IItemsIntegration itemsIntegration) : IRequestHandler<GetItemsByLocationQry, (IEnumerable<ItemsDTO> data, int count)>
{
    public async Task<(IEnumerable<ItemsDTO> data, int count)> Handle(GetItemsByLocationQry request, CancellationToken cancellationToken)
    {
        return await itemsIntegration.GetItemsByLocationDataGridAsync(request.Intent, request.locationId);
    }
}