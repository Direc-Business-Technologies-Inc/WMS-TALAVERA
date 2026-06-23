using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;

public record PostTransferOrderIRCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<bool>;

public class PostTransferOrderIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTransferOrderIRCmd, bool>
{
    public async Task<bool> Handle(PostTransferOrderIRCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveTOItemReceipt(request.Data);

        return result;
    }
}