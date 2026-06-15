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

public record GetItemsQry(DataGridIntent Intent) : IRequest<(IEnumerable<ItemsDTO> data, int count)>;

public class GetItemsQryHandler(IItemsIntegration itemsIntegration) : IRequestHandler<GetItemsQry, (IEnumerable<ItemsDTO> data, int count)>
{
    public async Task<(IEnumerable<ItemsDTO> data, int count)> Handle(GetItemsQry request, CancellationToken cancellationToken)
    {
        return await itemsIntegration.GetItemsDataGridAsync(request.Intent);
    }
}