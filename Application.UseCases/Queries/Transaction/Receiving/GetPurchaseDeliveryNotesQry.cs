using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.Receiving;

public record GetPurchaseDeliveryNotesQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseDeliveryNoteDataGridDTO> Data, int Count)>;

public class GetPurchaseDeliveryNotesQryHandler(
    IReceivingIntegration receivingIntegration) 
    : IRequestHandler<GetPurchaseDeliveryNotesQry, (IEnumerable<PurchaseDeliveryNoteDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<PurchaseDeliveryNoteDataGridDTO> Data, int Count)> Handle(GetPurchaseDeliveryNotesQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<PurchaseDeliveryNoteSAPDTO> Data, int Count) = await receivingIntegration.GetPurchaseDeliveryNotesListAsync(request.Intent);

        return (Data.Adapt<IEnumerable<PurchaseDeliveryNoteDataGridDTO>>(), Count);
    }
}