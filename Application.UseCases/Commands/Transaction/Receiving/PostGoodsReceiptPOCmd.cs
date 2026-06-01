using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving;

public record PostGoodsReceiptPOCmd(PurchaseOrderDTO Data) : ITransactionalRequest<bool>;

public class PostGoodsReceiptPOCmdHandler(IReceivingIntegration receivingIntegration) : IRequestHandler<PostGoodsReceiptPOCmd, bool>
{
    public async Task<bool> Handle(PostGoodsReceiptPOCmd request, CancellationToken cancellationToken)
    {
        PurchaseDeliveryNoteDTO payload =  request.Data.Adapt<PurchaseDeliveryNoteDTO>();

        bool result = await receivingIntegration.PostGoodsReceiptPOAsync(payload);

        return result;
    }
}
