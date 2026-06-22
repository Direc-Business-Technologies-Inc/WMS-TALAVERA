using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;

public record PostPurchaseOrderIRCmd(List<PostPurchaseOrderDTO> Data) : ITransactionalRequest<bool>;

public class PostPurchaseOrderIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostPurchaseOrderIRCmd, bool>
{
    public async Task<bool> Handle(PostPurchaseOrderIRCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SavePOItemReceipt(request.Data);

        return result;
    }
}