using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.System;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.System;
using Domain.Entities.Transaction.Common;
using Domain.Entities.ValueObjects.Transaction;
using Domain.ValueObjects.Transaction;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record CreateInventoryCountingSheetCmd(InventoryCountingSheetDTO Data) : ITransactionalRequest<bool>;

public class CreateInventoryCountingSheetCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo,
    IDocNumReadRepo docNumReadRepository)
    : IRequestHandler<CreateInventoryCountingSheetCmd, bool>
{
    public async Task<bool> Handle(CreateInventoryCountingSheetCmd request, CancellationToken cancellationToken)
    {
        DocumentTypeDEM docType = await appReadRepo.FirstOrDefaultAsync<DocumentTypeDEM>(x => x.Name.ToLower().Equals("counting sheet")) ?? throw new Exception("Document Type not found.");
        DocumentNumberDEM docNum = await docNumReadRepository.GetDocumentNumberEntityWithLockingAsync(docType.Id, appCommandRepo.GetDbContext());

        var dem = await appReadRepo.FirstOrDefaultAsync<InventoryCountingDocumentDEM>(d => d.Id == request.Data.InventoryCountingDocumentId, track: true, local: false);
        
        if (dem == null)
            throw new Exception("Inventory Counting Document not found.");

        var sheetLines = request.Data.SheetLines.Select(sl => new InventoryCountingSheetLineVO(
            sl.ItemCode,
            sl.ItemName,
            sl.Quantity,
            sl.UoMCode,
            sl.UoMValue,
            sl.UoMName
        )).ToList();

        var sheet = new InventoryCountingSheetVO(
            new AppDocNumVO(docNum.GenerateNextDocNum()),
            request.Data.InventoryCountingDocumentId,
            request.Data.Counter.UserId,
            request.Data.SubmittedDate,
            sheetLines
        );

        dem.AddSheet(sheet);

        appCommandRepo.Update(dem);

        return true;
    }
}
