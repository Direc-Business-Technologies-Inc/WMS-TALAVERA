using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;


public record UpdateStockTransferRequestCmd(StockTransferRequestInfoDTO dto) : IRequest<bool>;

public class UpdateStockTransferRequestCmdHandler(IStockTransferRequestIntegration integ) : IRequestHandler<UpdateStockTransferRequestCmd, bool>
{
    public async Task<bool> Handle(UpdateStockTransferRequestCmd request, CancellationToken cancellationToken)
    {
        return await integ.UpdateStockTransferRequest(request.dto);
    }
}
