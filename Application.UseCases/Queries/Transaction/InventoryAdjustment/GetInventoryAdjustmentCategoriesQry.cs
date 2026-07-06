using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryAdjustment;

public record GetInventoryAdjustmentCategoriesQry(DataGridIntent intent) 
    : IRequest<(IEnumerable<InventoryAdjustmentCategoryDTO>, int)>;

public class GetInventoryAdjustmentCategoriesQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetInventoryAdjustmentCategoriesQry, (IEnumerable<InventoryAdjustmentCategoryDTO>, int)>
{
    public Task<(IEnumerable<InventoryAdjustmentCategoryDTO>, int)> Handle(GetInventoryAdjustmentCategoriesQry request, CancellationToken cancellationToken)
    {
        return integration.GetInventoryAdjustmentCategoriesAsync(request.intent);
    }
}
