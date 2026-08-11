using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.SupplierReturn;

public record SubmitSupplierReturnRequestCmd(SupplierReturnDTO dto) : IRequest<bool>;

public class SubmitSupplierReturnRequestCmdHandler(ISupplierReturnIntegration integ) : IRequestHandler<SubmitSupplierReturnRequestCmd, bool>
{
    public async Task<bool> Handle(SubmitSupplierReturnRequestCmd request, CancellationToken cancellationToken)
    {
        return await integ.SubmitSupplierReturnForApproval(request.dto);
    }
}
