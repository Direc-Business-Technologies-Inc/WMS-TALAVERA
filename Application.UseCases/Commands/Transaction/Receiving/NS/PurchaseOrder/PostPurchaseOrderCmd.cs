using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using Shared.Libraries.ViewModel;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;

public record PostPurchaseOrderCmd(List<PurchaseOrderLineVM> Data) : ITransactionalRequest<bool>;

public class PostPurchaseOrderCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostPurchaseOrderCmd, bool>
{
    public async Task<bool> Handle(PostPurchaseOrderCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SavePOItemReceipt(request.Data);

        return result;
    }
}