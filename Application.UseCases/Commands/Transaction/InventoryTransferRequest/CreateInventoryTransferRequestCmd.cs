using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.InventoryTransferRequest;

public record CreateInventoryTransferRequestCmd(InventoryTransferRequestDTO data) : IRequest<bool>;

public class CreateInventoryTransferRequestCmdHandler(
    IInventoryTransferRequestIntegration integration)
    : IRequestHandler<CreateInventoryTransferRequestCmd, bool>
{
    public async Task<bool> Handle(CreateInventoryTransferRequestCmd request, CancellationToken cancellationToken)
    {
        return await integration.CreateInventoryTransferRequest(request.data);
    }
}