using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;

public record CreateStockTransferRequestCmd(StockTransferRequestInfoDTO dto) : IRequest<bool>;

public class CreateStockTransferRequestCmdHandler(IStockTransferRequestIntegration integ) : IRequestHandler<CreateStockTransferRequestCmd, bool>
{
    public async Task<bool> Handle(CreateStockTransferRequestCmd request, CancellationToken cancellationToken)
    {
        return await integ.CreateStockTransferRequest(request.dto);
    }
}