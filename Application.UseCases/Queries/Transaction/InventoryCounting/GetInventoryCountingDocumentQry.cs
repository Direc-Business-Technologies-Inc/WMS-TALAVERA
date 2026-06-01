using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Queries.Transaction.InventoryCounting;

public record GetInventoryCountingDocumentQry(Guid Id) : IRequest<InventoryCountingDocumentDTO?>;

public class GetInventoryCountingDocumentQryHandler(
    IInventoryCountingReadRepo readRepo)
    : IRequestHandler<GetInventoryCountingDocumentQry, InventoryCountingDocumentDTO?>
{
    public async Task<InventoryCountingDocumentDTO?> Handle(GetInventoryCountingDocumentQry request, CancellationToken cancellationToken)
    {
        return await readRepo.GetInventoryCountingDocument(request.Id);
    }
}
