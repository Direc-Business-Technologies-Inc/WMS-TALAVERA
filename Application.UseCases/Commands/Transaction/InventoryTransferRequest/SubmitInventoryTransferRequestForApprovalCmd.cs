using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.InventoryTransferRequest;

public record SubmitInventoryTransferRequestForApprovalCmd(InventoryTransferRequestDTO data) : IRequest<bool>;

public class SubmitInventoryTransferRequestForApprovalCmdHandler(
    IInventoryTransferRequestIntegration integration)
    : IRequestHandler<SubmitInventoryTransferRequestForApprovalCmd, bool>
{
    public Task<bool> Handle(SubmitInventoryTransferRequestForApprovalCmd request, CancellationToken cancellationToken)
    {
        return integration.SubmitInventoryTransferRequestForApproval(request.data);
    }
}