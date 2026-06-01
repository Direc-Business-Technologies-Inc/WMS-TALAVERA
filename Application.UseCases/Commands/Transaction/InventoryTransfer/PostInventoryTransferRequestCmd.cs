using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.UseCases.Commands.Transaction.InventoryTransfer;

public record PostInventoryTransferRequestCmd(InventoryTransferRequestDTO Data) : IRequest<int>;

public class PostInventoryTransferRequestCmdHandler(
    IInventoryTransferIntegration inventoryTransferIntegration
) : IRequestHandler<PostInventoryTransferRequestCmd, int>
{
    public async Task<int> Handle(PostInventoryTransferRequestCmd request, CancellationToken cancellationToken)
    {
        return await inventoryTransferIntegration.PostInventoryTransferRequest(request.Data);
    }
}