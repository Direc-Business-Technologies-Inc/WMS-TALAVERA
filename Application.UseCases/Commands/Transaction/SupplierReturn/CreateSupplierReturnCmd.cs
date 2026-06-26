using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record CreateSupplierReturnCmd(SupplierReturnDTO dto) : IRequest<bool>;

public class CreateSupplierReturnCmdHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<CreateSupplierReturnCmd, bool>
{
    public async Task<bool> Handle(CreateSupplierReturnCmd request, CancellationToken cancellationToken)
    {
        return await integration.CreateSupplierReturn(request.dto);
    }
}
