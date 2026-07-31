using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;


public record SubmitStockTransferRequestForApprovalCmd(StockTransferRequestInfoDTO dto) : IRequest<bool>;

public class SubmitStockTransferRequestForApprovalCmdHandler(IStockTransferRequestIntegration integ) : IRequestHandler<SubmitStockTransferRequestForApprovalCmd, bool>
{
    public async Task<bool> Handle(SubmitStockTransferRequestForApprovalCmd request, CancellationToken cancellationToken)
    {
        return await integ.SubmitStockTransferRequestForApproval(request.dto);
    }
}
