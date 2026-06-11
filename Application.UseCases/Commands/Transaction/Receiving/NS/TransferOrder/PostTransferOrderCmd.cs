using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;

public record PostTransferOrderCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<bool>;

public class PostTransferOrderCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTransferOrderCmd, bool>
{
    public async Task<bool> Handle(PostTransferOrderCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveTOItemReceipt(request.Data);

        return result;
    }
}