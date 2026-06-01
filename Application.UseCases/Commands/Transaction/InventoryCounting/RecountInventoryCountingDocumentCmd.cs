using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record RecountInventoryCountingDocumentCmd(Guid DocumentId) : ITransactionalRequest<bool>;

public class RecountInventoryCountingDocumentCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo)
    : IRequestHandler<RecountInventoryCountingDocumentCmd, bool>
{
    public async Task<bool> Handle(RecountInventoryCountingDocumentCmd request, CancellationToken cancellationToken)
    {
        var dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(d => d.Id == request.DocumentId, track: true, local: false);
        if (dem == null)
            throw new Exception("Inventory Counting Document not found.");

        if (dem.Status != InventoryCountingDocumentStatus.Saved)
            throw new Exception("Inventory Counting Document is not in SAVED state.");

        dem.UpdateStatus(InventoryCountingDocumentStatus.Recount);

        return true;
    }
}
