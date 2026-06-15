using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.Receiving;

public record CreateItemReceiptCmd(ItemReceiptDTO dto) : IRequest<bool>;

public class CreateItemReceiptCmdHandler(
    IReceivingIntegration receivingIntegration) : IRequestHandler<CreateItemReceiptCmd, bool>
{
    public async Task<bool> Handle(CreateItemReceiptCmd request, CancellationToken cancellationToken)
    {
        try
        {
            return await receivingIntegration.PostItemReceipt(request.dto);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
