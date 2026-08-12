using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record UpdateSupplierReturnCmd(SupplierReturnDTO dto) : IRequest<bool>;

public class UpdateSupplierReturnCmdHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<UpdateSupplierReturnCmd, bool>
{
    public async Task<bool> Handle(UpdateSupplierReturnCmd request, CancellationToken cancellationToken)
    {
        return await integration.UpdateSupplierReturn(request.dto);
    }
}
