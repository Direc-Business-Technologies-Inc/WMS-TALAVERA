using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record IgnoreInventoryCountingSheetCmd(Guid DocumentId, string SheetNo) : ITransactionalRequest<bool>;

public class IgnoreInventoryCountingSheetCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo)
    : IRequestHandler<IgnoreInventoryCountingSheetCmd, bool>
{
    public async Task<bool> Handle(IgnoreInventoryCountingSheetCmd request, CancellationToken cancellationToken)
    {
        var dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(d => d.Id == request.DocumentId);
        if (dem == null)
            throw new Exception("Inventory Counting Document not found.");

        var sheet = dem.Sheets.FirstOrDefault(s => s.SheetNo.Value == request.SheetNo);
        if (sheet == null)
            throw new Exception("Inventory Counting Sheet not found.");

        if (sheet.Status != InventoryCountingSheetStatus.Synced)
            throw new Exception("Inventory Counting Sheet is not in SYNCED state.");

        // Subtract sheet's line quantities from document's actual quantities (reverse of Sync)
        foreach (var sheetLine in sheet.SheetLines)
        {
            var docLine = dem.DocumentLines.FirstOrDefault(dl => dl.ItemCode == sheetLine.ItemCode && dl.UoMCode == sheetLine.UoMCode);
            if (docLine != null)
            {
                var newActualQty = docLine.ActualQuantity - sheetLine.Quantity;
                docLine.UpdateActualQuantity(newActualQty);
            }
            sheetLine.SetStatus(InventoryCountingSheetStatus.Ignore);
        }

        // Mark sheet status as IGNORE
        sheet.SetStatus(InventoryCountingSheetStatus.Ignore);

        appCommandRepo.Update(dem);

        return true;
    }
}
