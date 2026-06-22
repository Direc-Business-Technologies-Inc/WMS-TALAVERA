using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;

public record PostTransferOrderIFCmd(List<PostTransferOrderDTO> Data) : ITransactionalRequest<bool>;

public class PostTransferOrderIFCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostTransferOrderIFCmd, bool>
{
    public async Task<bool> Handle(PostTransferOrderIFCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveTOItemFulfillment(request.Data);

        return result;
    }
}