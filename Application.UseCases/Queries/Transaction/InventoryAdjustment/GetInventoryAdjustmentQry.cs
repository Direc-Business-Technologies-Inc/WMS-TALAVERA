using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.InventoryAdjustment;

public record GetInventoryAdjustmentQry(string id) : IRequest<InventoryAdjustmentDTO?>;

public class GetInventoryAdjustmentQryHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<GetInventoryAdjustmentQry, InventoryAdjustmentDTO?>
{
    public async Task<InventoryAdjustmentDTO?> Handle(GetInventoryAdjustmentQry request, CancellationToken cancellationToken)
    {
        var dto = await integration.GetInventoryAdjustmentAsync(request.id);
        if (dto == null) return null;

        var lines = await integration.GetInventoryAdjustmentLinesAsync(request.id);
        dto.Lines = [..lines];

        return dto;
    }
}
