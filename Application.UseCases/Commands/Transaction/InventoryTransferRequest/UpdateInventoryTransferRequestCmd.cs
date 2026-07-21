using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.InventoryTransferRequest;



public record UpdateInventoryTransferRequestCmd(InventoryTransferRequestDTO data) : IRequest<bool>;

public class UpdateInventoryTransferRequestCmdHandler(
    IInventoryTransferRequestIntegration integration)
    : IRequestHandler<UpdateInventoryTransferRequestCmd, bool>
{
    public Task<bool> Handle(UpdateInventoryTransferRequestCmd request, CancellationToken cancellationToken)
    {
        return integration.UpdateInventoryTransferRequest(request.data);
    }
}