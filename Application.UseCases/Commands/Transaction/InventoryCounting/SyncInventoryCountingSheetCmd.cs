using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record SyncInventoryCountingSheetCmd(Guid DocumentId, string SheetNo) : ITransactionalRequest<bool>;

public class SyncInventoryCountingSheetCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo) 
    : IRequestHandler<SyncInventoryCountingSheetCmd, bool>
{
    public async Task<bool> Handle(SyncInventoryCountingSheetCmd request, CancellationToken cancellationToken)
    {
        var dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(d => d.Id == request.DocumentId, track: true, local: false);
        if (dem == null)
            throw new Exception("Inventory Counting Document not found.");

        var sheet = dem.Sheets.FirstOrDefault(s => s.SheetNo.Value == request.SheetNo);
        if (sheet == null)
            throw new Exception("Inventory Counting Sheet not found.");

        if (sheet.Status != InventoryCountingSheetStatus.Async)
            throw new Exception("Inventory Counting Sheet is not in ASYNC state.");

        // Add sheet's line quantities to document's actual quantities
        foreach (var sheetLine in sheet.SheetLines)
        {
            var docLine = dem.DocumentLines.FirstOrDefault(dl => dl.ItemCode == sheetLine.ItemCode && dl.UoMCode == sheetLine.UoMCode);
            if (docLine != null)
            {
                var newActualQty = docLine.ActualQuantity + sheetLine.Quantity;
                docLine.UpdateActualQuantity(newActualQty);
            }
            sheetLine.SetStatus(InventoryCountingSheetStatus.Synced);
        }

        // Mark sheet status as SYNCED
        sheet.SetStatus(InventoryCountingSheetStatus.Synced);

        appCommandRepo.Update(dem);

        return true;
    }
}
