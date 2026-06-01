using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record SaveInventoryCountingDocumentCmd(Guid DocumentId) : ITransactionalRequest<bool>;

public class SaveInventoryCountingDocumentCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo)
    : IRequestHandler<SaveInventoryCountingDocumentCmd, bool>
{
    public async Task<bool> Handle(SaveInventoryCountingDocumentCmd request, CancellationToken cancellationToken)
    {
        var dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(d => d.Id == request.DocumentId, track: true, local: false);
        if (dem == null)
            throw new Exception("Inventory Counting Document not found.");

        if (dem.Status != InventoryCountingDocumentStatus.Open)
            throw new Exception("Inventory Counting Document is not in OPEN state.");

        dem.UpdateStatus(InventoryCountingDocumentStatus.Saved);

        return true;
    }
}
