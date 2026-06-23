using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.InventoryAdjustment;

public record CreateInventoryAdjustmentCmd(InventoryAdjustmentDTO data) : IRequest<bool>;

public class CreateInventoryAdjustmentCmdHandler(IInventoryAdjustmentIntegration integration)
    : IRequestHandler<CreateInventoryAdjustmentCmd, bool>
{
    public async Task<bool> Handle(CreateInventoryAdjustmentCmd request, CancellationToken cancellationToken)
    {
        return await integration.CreateInventoryAdjustment(request.data);
    }
}
