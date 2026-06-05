using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;

public record PostTransferOrderCmd(List<TransferOrderLineVM> Data) : ITransactionalRequest<bool>;

public class PostTransferOrderCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTransferOrderCmd, bool>
{
    public async Task<bool> Handle(PostTransferOrderCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveTOItemReceipt(request.Data);

        return result;
    }
}