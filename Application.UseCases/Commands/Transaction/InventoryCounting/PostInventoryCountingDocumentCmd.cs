using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record PostInventoryCountingDocumentCmd(Guid DocumentId) : ITransactionalRequest<bool>;

public class PostInventoryCountingDocumentCmdHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo,
    IInventoryCountingIntegration inventoryCountingIntegration,
    IInventoryCountingReadRepo readRepo)
    : IRequestHandler<PostInventoryCountingDocumentCmd, bool>
{
    public async Task<bool> Handle(PostInventoryCountingDocumentCmd request, CancellationToken cancellationToken)
    {
        InventoryCountingDocumentDTO? dto = await readRepo.GetInventoryCountingDocument(request.DocumentId) ?? throw new Exception("Inventory Counting Document not found.");

        if (dto.Status != InventoryCountingDocumentStatus.Saved)
            throw new Exception("Inventory Counting Document is not in SAVED state.");

        bool postingSuccess = await inventoryCountingIntegration.PostInventoryCountings(dto);

        if (postingSuccess)
        {
            InventoryCountingDocumentDEM? dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(x => x.Id == request.DocumentId, true, false);

            if (dem is not null)
            {
                dem.UpdateStatus(InventoryCountingDocumentStatus.Posted);
                appCommandRepo.Update(dem);
            }
        }

        return true;
    }
}
